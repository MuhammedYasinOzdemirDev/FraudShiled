using Analiz.Application.Exceptions;
using Analiz.Application.Extensions;
using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using Analiz.Domain.Events;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Analiz.Application.Services;

public class RiskScoringService : IRiskScoringService
{
    private readonly IModelService _modelService;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<RiskScoringService> _logger;
    private readonly IPublisher _publisher;
    private readonly RiskScoringConfiguration _configuration;

    private const double EarthRadiusKm = 6371;
    private const double MaxLocationDistanceKm = 10.0;

    public RiskScoringService(
        IModelService modelService,
        IFeatureExtractionService featureExtractor,
        ITransactionRepository transactionRepository,
        ILogger<RiskScoringService> logger,
        IPublisher publisher,
        IOptions<RiskScoringConfiguration> configuration)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<RiskScore> CalculateRiskScoreAsync(TransactionData data)
    {
        try
        {
            _logger.LogInformation("Starting risk calculation for transaction {TransactionId}", data.TransactionId);

            var features = await _featureExtractor.ExtractFeaturesAsync(data);
            var (baseScore, riskFactors) = await CalculateScoreComponentsAsync(data, features);
            var adjustedScore = CalculateAdjustedScore(baseScore, riskFactors);
            var riskScore = CreateRiskScore(adjustedScore, riskFactors);

            await PublishRiskScoreEventsAsync(data, riskScore, riskFactors);

            _logger.LogInformation("Completed risk calculation for transaction {TransactionId}", data.TransactionId);
            return riskScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate risk score for transaction {TransactionId}", data.TransactionId);
            throw new RiskScoringException($"Error calculating risk score for transaction {data.TransactionId}", ex);
        }
    }

    public async Task<RiskProfile> GetUserRiskProfileAsync(string userId)
    {
        try
        {
            var transactions = await _transactionRepository
                .GetUserTransactionsAsync(userId, _configuration.ProfileLookbackPeriod);

            if (!transactions.Any())
            {
                return CreateEmptyRiskProfile(userId);
            }

            var transactionDataList = transactions.Select(t => t.ToTransactionData()).ToList();
            await _featureExtractor.ExtractBatchFeaturesAsync(transactionDataList);

            var profile = new RiskProfile
            {
                UserId = userId,
                AverageRiskScore = CalculateAverageRiskScore(transactions),
                TransactionCount = transactions.Count,
                HighRiskTransactionCount = CountHighRiskTransactions(transactions),
                CommonRiskFactors = GetCommonRiskFactors(transactions),
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Generated risk profile for user {UserId}", userId);
            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating risk profile for user {UserId}", userId);
            throw new Exception($"Failed to generate risk profile for user {userId}", ex);
        }
    }

    public async Task<bool> UpdateRiskThresholdsAsync(RiskThresholds thresholds)
    {
        try
        {
            if (!ValidateThresholds(thresholds))
            {
                _logger.LogWarning("Invalid risk thresholds provided");
                return false;
            }

            _configuration.UpdateThresholds(thresholds);
            await _publisher.Publish(new RiskThresholdsUpdatedEvent(thresholds));
            
            _logger.LogInformation("Risk thresholds updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update risk thresholds");
            throw new Exception("Error updating risk thresholds", ex);
        }
    }

    public async Task<List<RiskFactor>> GetRiskFactorsAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
            if (transaction == null)
            {
                throw new TransactionNotFoundException(transactionId);
            }

            var transactionData = transaction.ToTransactionData();
            var features = await _featureExtractor.ExtractFeaturesAsync(transactionData);

            return await CalculateRiskFactorsAsync(transactionData, features);
        }
        catch (TransactionNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating risk factors for transaction {TransactionId}", transactionId);
            throw new Exception($"Failed to calculate risk factors for transaction {transactionId}", ex);
        }
    }

    public async Task<RiskTrends> AnalyzeRiskTrendsAsync(string userId, TimeSpan period)
    {
        try
        {
            var transactions = await _transactionRepository.GetUserTransactionsAsync(userId, period);

            if (!transactions.Any())
            {
                return CreateEmptyRiskTrends(userId, period);
            }

            return new RiskTrends
            {
                UserId = userId,
                Period = period,
                DailyScores = CalculateDailyRiskScores(transactions),
                RiskFactorFrequency = CalculateRiskFactorFrequency(transactions),
                TrendLine = CalculateTrendLine(transactions),
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing risk trends for user {UserId}", userId);
            throw new Exception($"Failed to analyze risk trends for user {userId}", ex);
        }
    }

    private async Task<(double BaseScore, List<RiskFactor> RiskFactors)> CalculateScoreComponentsAsync(
        TransactionData data, 
        FeatureSet features)
    {
        var baseScoreTask = CalculateBaseScoreAsync(features);
        var riskFactorsTask = CalculateRiskFactorsAsync(data, features);

        await Task.WhenAll(baseScoreTask, riskFactorsTask);

        return (await baseScoreTask, await riskFactorsTask);
    }

    private async Task<double> CalculateBaseScoreAsync(FeatureSet features)
    {
        var modelInput = new ModelInput { Features = features.ToVector() };
        var evaluationResult = await _modelService.EvaluateModelAsync(new EvaluationRequest 
        { 
            ModelName = "RiskScoring",
            Version = "v1",
            ModelType = ModelType.LightGBM,
            EvaluationData = new List<TransactionData> { new() },
            Labels = new List<bool> { false }
        });
        return evaluationResult.Metrics.AUC;
    }

    private async Task<List<RiskFactor>> CalculateRiskFactorsAsync(
        TransactionData data, 
        FeatureSet features)
    {
        var riskFactors = new List<RiskFactor>();
        var userProfile = await GetUserRiskProfileAsync(data.UserId);

        await Task.WhenAll(
            CheckAmountBasedRiskAsync(data, userProfile, riskFactors),
            CheckLocationBasedRiskAsync(data, riskFactors),
            CheckVelocityBasedRiskAsync(data, riskFactors)
        );

        return riskFactors;
    }

    private async Task CheckAmountBasedRiskAsync(
        TransactionData data,
        RiskProfile userProfile,
        List<RiskFactor> riskFactors)
    {
        if (CalculationExtensions.CompareAmounts(data.Amount, 
            userProfile.AverageTransactionAmount * _configuration.UnusualAmountMultiplier) > 0)
        {
            riskFactors.Add(new RiskFactor
            {
                Code = RiskFactorCodes.UnusualAmount,
                Description = "Transaction amount significantly higher than user average",
                Weight = 1.5
            });
        }
    }

    private async Task CheckLocationBasedRiskAsync(
        TransactionData data,
        List<RiskFactor> riskFactors)
    {
        if (await IsUnusualLocation(data.UserId, data.Location))
        {
            riskFactors.Add(new RiskFactor
            {
                Code = RiskFactorCodes.UnusualLocation,
                Description = "Transaction from unusual location",
                Weight = 1.3
            });
        }
    }

    private async Task CheckVelocityBasedRiskAsync(
        TransactionData data,
        List<RiskFactor> riskFactors)
    {
        if (await ExceedsVelocityThresholds(data.UserId))
        {
            riskFactors.Add(new RiskFactor
            {
                Code = RiskFactorCodes.VelocityCheck,
                Description = "Transaction velocity exceeds thresholds",
                Weight = 1.4
            });
        }
    }

    private async Task<bool> IsUnusualLocation(string userId, Location location)
    {
        var recentTransactions = await _transactionRepository
            .GetUserTransactionsAsync(userId, TimeSpan.FromDays(30));

        if (!recentTransactions.Any())
            return false;

        var commonLocations = recentTransactions
            .Select(t => t.Location)
            .Where(l => l != null)
            .ToList();

        return !IsLocationWithinCommonAreas(location, commonLocations);
    }

    private async Task<bool> ExceedsVelocityThresholds(string userId)
    {
        var recentTransactions = await _transactionRepository
            .GetUserTransactionsAsync(userId, 
                TimeSpan.FromMinutes(_configuration.VelocityCheckPeriodMinutes));

        return recentTransactions.Count > _configuration.MaxTransactionsPerPeriod;
    }

    private double CalculateAdjustedScore(double baseScore, List<RiskFactor> riskFactors)
    {
        var adjustedScore = baseScore;
        foreach (var factor in riskFactors)
        {
            adjustedScore *= factor.Weight;
        }
        return Math.Min(1.0, adjustedScore);
    }

    private Dictionary<DateTime, double> CalculateDailyRiskScores(IEnumerable<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => t.TransactionTime.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Average(t => t.RiskScore?.Score ?? 0));
    }

    private Dictionary<string, int> CalculateRiskFactorFrequency(IEnumerable<Transaction> transactions)
    {
        return transactions
            .SelectMany(t => t.RiskScore?.Factors ?? Enumerable.Empty<string>())
            .GroupBy(f => f)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private TrendLineData CalculateTrendLine(IEnumerable<Transaction> transactions)
    {
        var points = transactions
            .Select(t => new Point
            {
                X = (t.TransactionTime - DateTime.MinValue).TotalDays,
                Y = t.RiskScore?.Score ?? 0
            })
            .ToList();

        if (!points.Any())
            return new TrendLineData();

        var (slope, intercept) = CalculateLinearRegression(points);
        var rSquared = CalculateRSquared(points, slope, intercept);

        return new TrendLineData
        {
            Slope = slope,
            Intercept = intercept,
            RSquared = rSquared
        };
    }

    private (double Slope, double Intercept) CalculateLinearRegression(List<Point> points)
    {
        var n = points.Count;
        var sumX = points.Sum(p => p.X);
        var sumY = points.Sum(p => p.Y);
        var sumXY = points.Sum(p => p.X * p.Y);
        var sumXX = points.Sum(p => p.X * p.X);

        var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        var intercept = (sumY - slope * sumX) / n;

        return (slope, intercept);
    }

    private double CalculateRSquared(List<Point> points, double slope, double intercept)
    {
        var yMean = points.Average(p => p.Y);
        var totalSS = points.Sum(p => Math.Pow(p.Y - yMean, 2));
        var residualSS = points.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2));

        return 1 - (residualSS / totalSS);
    }

    private bool IsLocationWithinCommonAreas(Location location, List<Location> commonLocations)
    {
        return commonLocations.Any(l => CalculateDistance(location, l) <= MaxLocationDistanceKm);
    }

    private double CalculateDistance(Location loc1, Location loc2)
    {
        var dLat = ToRad(loc2.Latitude - loc1.Latitude);
        var dLon = ToRad(loc2.Longitude - loc1.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(loc1.Latitude)) * Math.Cos(ToRad(loc2.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private double ToRad(double degrees) => degrees * Math.PI / 180;
    private static RiskProfile CreateEmptyRiskProfile(string userId)
    {
        return new RiskProfile
        {
            UserId = userId,
            AverageRiskScore = 0,
            TransactionCount = 0,
            HighRiskTransactionCount = 0,
            CommonRiskFactors = new Dictionary<string, int>(),
            LastUpdated = DateTime.UtcNow
        };
    }

    private static RiskTrends CreateEmptyRiskTrends(string userId, TimeSpan period)
    {
        return new RiskTrends
        {
            UserId = userId,
            Period = period,
            DailyScores = new Dictionary<DateTime, double>(),
            RiskFactorFrequency = new Dictionary<string, int>(),
            TrendLine = new TrendLineData(),
            LastUpdated = DateTime.UtcNow
        };
    }

    private RiskScore CreateRiskScore(double adjustedScore, List<RiskFactor> riskFactors)
    {
        return RiskScore.Create(
            adjustedScore,
            riskFactors.Select(f => f.Description).ToList());
    }

    private async Task PublishRiskScoreEventsAsync(
        TransactionData data,
        RiskScore riskScore,
        List<RiskFactor> riskFactors)
    {
        if (riskScore.Level >= RiskLevel.High)
        {
            await _publisher.Publish(new HighRiskTransactionDetectedEvent(
                data.TransactionId,
                data.UserId,
                riskScore,
                riskFactors.Select(f => f.Description).ToList()));
        }
    }

    private static double CalculateAverageRiskScore(IEnumerable<Transaction> transactions)
    {
        return transactions.Average(t => t.RiskScore?.Score ?? 0);
    }

    private static int CountHighRiskTransactions(IEnumerable<Transaction> transactions)
    {
        return transactions.Count(t => t.RiskScore?.Level >= RiskLevel.High);
    }

    private Dictionary<string, int> GetCommonRiskFactors(IEnumerable<Transaction> transactions)
    {
        return transactions
            .SelectMany(t => t.RiskScore?.Factors ?? Enumerable.Empty<string>())
            .GroupBy(f => f)
            .ToDictionary(g => g.Key, g => g.Count())
            .OrderByDescending(kvp => kvp.Value)
            .Take(5)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private bool ValidateThresholds(RiskThresholds thresholds)
    {
        if (thresholds == null)
        {
            _logger.LogWarning("Null thresholds provided");
            return false;
        }

        if (thresholds.LowRisk >= thresholds.MediumRisk ||
            thresholds.MediumRisk >= thresholds.HighRisk ||
            thresholds.HighRisk >= thresholds.CriticalRisk)
        {
            _logger.LogWarning("Invalid threshold values: thresholds must be strictly increasing");
            return false;
        }

        if (thresholds.LowRisk < 0 || thresholds.CriticalRisk > 1)
        {
            _logger.LogWarning("Invalid threshold range: values must be between 0 and 1");
            return false;
        }

        return true;
    }

    private static class RiskFactorCodes
    {
        public const string UnusualAmount = "UNUSUAL_AMOUNT";
        public const string UnusualLocation = "UNUSUAL_LOCATION";
        public const string VelocityCheck = "VELOCITY_CHECK";
    }

    private static class CalculationExtensions
    {
        public static double CompareAmounts(decimal amount1, double amount2)
        {
            return Convert.ToDouble(amount1).CompareTo(amount2);
        }
    }

    private class Point
    {
        public double X { get; init; }
        public double Y { get; init; }
    }
}
