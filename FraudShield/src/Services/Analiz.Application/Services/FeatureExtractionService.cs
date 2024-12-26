using System.Collections;
using System.Reflection;
using Analiz.Application.Exceptions;
using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML;

namespace Analiz.Application.Services;

public class FeatureExtractionService : IFeatureExtractionService
{
private readonly ITransactionRepository _transactionRepository;
    private readonly IFeatureConfigurationRepository _configurationRepository;
    private readonly MLContext _mlContext;
    private readonly ILogger<FeatureExtractionService> _logger;
    private readonly FeatureConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly IModelPredictionService _modelPredictionService;

    public FeatureExtractionService(
        ITransactionRepository transactionRepository,
        IFeatureConfigurationRepository configurationRepository,
        MLContext mlContext,
        ILogger<FeatureExtractionService> logger,
        IOptions<FeatureConfiguration> configuration,
        IMemoryCache cache,
        IModelPredictionService modelService)
    {
        _transactionRepository = transactionRepository;
        _configurationRepository = configurationRepository;
        _mlContext = mlContext;
        _logger = logger;
        _configuration = configuration.Value;
        _cache = cache;
        _modelPredictionService = modelService;
    }

    public async Task<bool> UpdateFeatureConfigurationAsync(FeatureConfig config)
    {
        try
        {
            if (!ValidateFeatureConfiguration(config))
            {
                return false;
            }

            var existingConfig = await GetActiveConfigurationAsync();
            if (existingConfig == null)
            {
                return false;
            }

            existingConfig.UpdateFeatures(config.EnabledFeatures);
            existingConfig.UpdateSettings(ConvertToFeatureSettings(config.FeatureSettings));

            if (config.NormalizationParameters != null)
            {
                var updatedConfig = FeatureConfiguration.Create(
                    config.EnabledFeatures,
                    config.FeatureSettings,
                    config.NormalizationParameters);

                await _configurationRepository.UpdateAsync(updatedConfig);
            }

            ClearCaches();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feature configuration");
            return false;
        }
    }

    private Dictionary<string, FeatureSetting> ConvertToFeatureSettings(
        Dictionary<string, FeatureSetting> configSettings)
    {
        return configSettings.ToDictionary(
            kvp => kvp.Key,
            kvp => new FeatureSetting
            {
                Name = kvp.Value.Name,
                Type = kvp.Value.Type, // Direct assignment since both are FeatureType
                IsRequired = kvp.Value.IsRequired,
                TransformationType = kvp.Value.TransformationType,
                ValidationRules = kvp.Value.ValidationRules,
                Parameters = kvp.Value.Parameters
            });
    }

    private async Task<FeatureConfiguration> GetActiveConfigurationAsync()
    {
        const string cacheKey = "ActiveFeatureConfiguration";

        if (_cache.TryGetValue(cacheKey, out FeatureConfiguration config))
            return config;

        config = await _configurationRepository.GetActiveConfigurationAsync();
        if (config != null)
        {
            _cache.Set(cacheKey, config, TimeSpan.FromHours(1));
        }

        return config;
    }

    private void ClearCaches()
    {
        var cacheKeys = new[]
        {
            "FeatureImportance_",
            "ActiveFeatureConfiguration",
            "FeatureStatistics_"
        };

        if (_cache is MemoryCache memoryCache)
        {
            var entriesField = typeof(MemoryCache).GetField("_entries", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (entriesField?.GetValue(memoryCache) is IDictionary cacheEntries)
            {
                foreach (DictionaryEntry entry in cacheEntries)
                {
                    var key = entry.Key?.ToString();
                    if (key != null && cacheKeys.Any(prefix => key.StartsWith(prefix)))
                    {
                        _cache.Remove(key);
                    }
                }
            }
        }
    }

    public async Task<FeatureSet> ExtractFeaturesAsync(TransactionData data)
    {
        try
        {
            _logger.LogInformation("Extracting features for transaction {TransactionId}",
                data.TransactionId);

            var features = new Dictionary<string, double>();
            var metadata = new Dictionary<string, string>
            {
                ["TransactionId"] = data.TransactionId.ToString(),
                ["ExtractedAt"] = DateTime.UtcNow.ToString("O"),
                ["UserId"] = data.UserId,
                ["TransactionType"] = data.Type.ToString()
            };

            await ExtractBasicFeaturesAsync(features, data);
            await ExtractBehavioralFeaturesAsync(features, data.UserId);
            await ExtractLocationFeaturesAsync(features, data.Location);
            await ExtractDeviceFeaturesAsync(features, data.DeviceInfo);
            await ExtractTemporalFeaturesAsync(features, data.Timestamp);

            // Add feature extraction metadata
            metadata["FeatureCount"] = features.Count.ToString();
            metadata["EnabledFeatureGroups"] = string.Join(",",
                _configuration.EnabledFeatures.Where(f => f.Value).Select(f => f.Key));

            return FeatureSet.Create(features, metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting features for transaction {TransactionId}",
                data.TransactionId);
            throw;
        }
    }

    public async Task<List<FeatureSet>> ExtractBatchFeaturesAsync(List<TransactionData> data)
    {
        try
        {
            _logger.LogInformation("Processing batch feature extraction for {Count} transactions",
                data.Count);

            var batchSize = 100;
            var results = new List<FeatureSet>();

            foreach (var batch in data.Chunk(batchSize))
            {
                var tasks = batch.Select(ExtractFeaturesAsync);
                var batchResults = await Task.WhenAll(tasks);
                results.AddRange(batchResults);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch feature extraction");
            throw;
        }
    }


    private async Task ExtractBasicFeaturesAsync(Dictionary<string, double> features, TransactionData data)
    {
        if (!_configuration.EnabledFeatures.GetValueOrDefault("BasicFeatures", true))
            return;

        features["Amount"] = Convert.ToDouble(data.Amount);
        features["TransactionType"] = (double)data.Type;
    }

    private async Task ExtractBehavioralFeaturesAsync(Dictionary<string, double> features, string userId)
    {
        if (!_configuration.EnabledFeatures.GetValueOrDefault("BehavioralFeatures", true))
            return;

        var recentTransactions = await _transactionRepository
            .GetUserTransactionsAsync(userId, TimeSpan.FromDays(30));

        if (!recentTransactions.Any())
            return;

        features["TransactionCount"] = recentTransactions.Count;
        features["AverageAmount"] = recentTransactions.Average(t => Convert.ToDouble(t.Amount));
    }

    private async Task ExtractLocationFeaturesAsync(Dictionary<string, double> features, Location location)
    {
        if (!_configuration.EnabledFeatures.GetValueOrDefault("LocationFeatures", true))
            return;

        features["Latitude"] = location.Latitude;
        features["Longitude"] = location.Longitude;
    }

    private async Task ExtractDeviceFeaturesAsync(Dictionary<string, double> features, DeviceInfo deviceInfo)
    {
        if (!_configuration.EnabledFeatures.GetValueOrDefault("DeviceFeatures", true))
            return;

        foreach (var setting in _configuration.FeatureSettings
                     .Where(s => s.Value.Type == (FeatureType)FeatureCategory.Device))
        {
            features[$"Device_{setting.Key}"] =
                CalculateDeviceFeature(deviceInfo, setting.Value);
        }
    }

    private async Task ExtractTemporalFeaturesAsync(Dictionary<string, double> features, DateTime timestamp)
    {
        if (!_configuration.EnabledFeatures.GetValueOrDefault("TemporalFeatures", true))
            return;

        features["Hour"] = timestamp.Hour;
        features["DayOfWeek"] = (int)timestamp.DayOfWeek;
        features["IsWeekend"] = timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1 : 0;
    }

    private double CalculateDeviceFeature(DeviceInfo deviceInfo, FeatureSetting setting)
    {
        var parameters = setting.Parameters ?? new Dictionary<string, double>();

        // Apply feature-specific calculations based on setting type
        switch (setting.TransformationType?.ToLower())
        {
            case "binary":
                return deviceInfo.DeviceType == setting.Name ? 1.0 : 0.0;
            case "categorical":
                return GetCategoricalFeatureValue(deviceInfo, setting);
            default:
                return 0.0;
        }
    }

    private double GetCategoricalFeatureValue(DeviceInfo deviceInfo, FeatureSetting setting)
    {
        // Implementation for categorical feature calculation
        return 0.0;
    }

    private async Task<FeatureConfiguration> GetActiveConfiguration()
    {
        const string cacheKey = "ActiveFeatureConfiguration";

        if (_cache.TryGetValue(cacheKey, out FeatureConfiguration config))
            return config;

        // Here you would typically retrieve from your repository
        // For now, return null or throw appropriate exception
        return null;
    }

    private double CalculateFeatureImportanceScore(ITransformer transformer)
    {
        try
        {
            // Implement feature importance calculation logic using ML.NET
            var featureImportances = _mlContext.Data
                .CreateEnumerable<FeatureImportanceData>(
                    transformer.Transform(_mlContext.Data.LoadFromEnumerable(new List<FeatureImportanceData>())),
                    reuseRowObject: false)
                .ToList();

            return featureImportances.Average(f => f.Importance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating feature importance score");
            return 0.0;
        }
    }

    private Dictionary<string, double> GetFeatureStatistics(ITransformer transformer)
    {
        try
        {
            var statistics = new Dictionary<string, double>();

            // Calculate basic statistics from the transformer
            // This would typically include mean, variance, etc.

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating feature statistics");
            return new Dictionary<string, double>();
        }
    }

    public async Task<FeatureImportance> GetFeatureImportanceAsync(string modelName)
    {
        var cacheKey = $"FeatureImportance_{modelName}";

        if (_cache.TryGetValue(cacheKey, out FeatureImportance importance))
        {
            _logger.LogInformation("Retrieved feature importance from cache for model {ModelName}", modelName);
            return importance;
        }

        try
        {
            var transformer = await _modelPredictionService.GetModelTransformerAsync(modelName);
            if (transformer == null)
            {
                throw new ModelNotFoundException($"Model not found: {modelName}");
            }

            var statistics = GetFeatureStatistics(transformer);
            var score = CalculateFeatureImportanceScore(transformer);

            var featureImportance = FeatureImportance.Create(
                modelName,
                "feature_importance_analysis",
                score,
                FeatureCategory.Transaction,
                statistics
            );

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1))
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, featureImportance, cacheOptions);

            _logger.LogInformation("Calculated and cached feature importance for model {ModelName}", modelName);
            return featureImportance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating feature importance for model {ModelName}", modelName);
            throw new Exception($"Failed to calculate feature importance for model {modelName}", ex);
        }
    }

    

    private bool ValidateFeatureConfiguration(FeatureConfig config)
    {
        if (config == null)
            return false;

        if (config.EnabledFeatures == null || !config.EnabledFeatures.Any())
            return false;

        if (config.FeatureSettings == null || !config.FeatureSettings.Any())
            return false;

        foreach (var setting in config.FeatureSettings)
        {
            if (string.IsNullOrWhiteSpace(setting.Value.Name))
                return false;

            if (setting.Value.IsRequired && !config.EnabledFeatures.ContainsKey(setting.Key))
                return false;
        }

        return true;
    }




    private async Task UpdateNormalizationParameters(
        FeatureConfiguration config,
        Dictionary<string, double> parameters)
    {
        // Create a new feature configuration with updated parameters
        var updatedConfig = FeatureConfiguration.Create(
            config.EnabledFeatures,
            config.FeatureSettings,
            parameters);

        // Save the updated configuration through your repository
        await _configurationRepository.UpdateAsync(updatedConfig);
    }

    private async Task InvalidateFeatureCaches()
    {
        var cachePatterns = new[]
        {
            "FeatureImportance_*",
            "ActiveFeatureConfiguration",
            "FeatureStatistics_*"
        };

        foreach (var pattern in cachePatterns)
        {
            var cacheEntry = _cache.Get(pattern);
            if (cacheEntry != null)
            {
                _cache.Remove(pattern);
            }
        }

        await Task.CompletedTask; // For async consistency
    }

    private FeatureType MapFeatureType(string sourceType)
    {
        return sourceType?.ToLower() switch
        {
            "numeric" => FeatureType.Numeric,
            "categorical" => FeatureType.Categorical,
            "binary" => FeatureType.Binary,
            "datetime" => FeatureType.DateTime,
            "text" => FeatureType.Text,
            "derived" => FeatureType.Derived,
            _ => throw new ArgumentException($"Unsupported feature type: {sourceType}")
        };
    }

    private Dictionary<string, FeatureSetting> MapFeatureSettings(
        Dictionary<string, FeatureSetting> settings)
    {
        if (settings == null)
            return new Dictionary<string, FeatureSetting>();

        return settings.ToDictionary(
            kvp => kvp.Key,
            kvp => new FeatureSetting
            {
                Name = kvp.Value.Name,
                Type = kvp.Value.Type,  // Direct assignment since both are FeatureType
                IsRequired = kvp.Value.IsRequired,
                TransformationType = kvp.Value.TransformationType,
                ValidationRules = new Dictionary<string, string>(kvp.Value.ValidationRules ?? 
                                                                 new Dictionary<string, string>()),
                Parameters = new Dictionary<string, double>(kvp.Value.Parameters ?? 
                                                            new Dictionary<string, double>())
            });
    }
    private void ClearFeatureCaches()
    {
        var cacheKeys = new[]
        {
            "FeatureImportance_",
            "ActiveFeatureConfiguration",
            "FeatureStatistics_"
        };

        if (_cache is MemoryCache memoryCache)
        {
            var entriesField = typeof(MemoryCache).GetField("_entries", 
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (entriesField?.GetValue(memoryCache) is IDictionary cacheEntries)
            {
                foreach (DictionaryEntry entry in cacheEntries)
                {
                    var key = entry.Key?.ToString();
                    if (key != null && cacheKeys.Any(prefix => key.StartsWith(prefix)))
                    {
                        _cache.Remove(key);
                        _logger.LogDebug("Removed cache entry with key: {CacheKey}", key);
                    }
                }
            }
        }

        _logger.LogInformation("Cache invalidation completed for feature-related entries");
    }


    private class FeatureImportanceData
    {
        public float Importance { get; set; }
        public string FeatureName { get; set; }
    }
}