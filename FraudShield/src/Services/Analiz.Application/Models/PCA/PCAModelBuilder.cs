using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.TimeSeries;
using Analiz.Application.Exceptions;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Services;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
using Analiz.ML.Models.PCA;

public class PCAModelBuilder : IModelBuilder
{
    private readonly MLContext _mlContext;
    private readonly PCAConfiguration _configuration;
    private readonly ILogger<ModelService> _logger;

    public PCAModelBuilder(
        MLContext mlContext,
        PCAConfiguration configuration,
        ILogger<ModelService> logger)
    {
        _mlContext = mlContext;
        _configuration = configuration;
        _logger = logger;
    }

public IEstimator<ITransformer> BuildPipeline()
{
    try
    {
        _logger.LogInformation("Building PCA pipeline...");

        // 0. (Opsiyonel) Custom mapping ile Time ve Amount özelliklerini oluşturun.
        var timeMapping = _mlContext.Transforms.CustomMapping<CreditCardMLData, TimeFeatures>(
            (input, output) =>
            {
                output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / _configuration.TimeScaleFactor);
                output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / _configuration.TimeScaleFactor);
                output.DayFeature = (float)((input.Time / (24 * 3600)) % 7);
                output.HourFeature = (float)((input.Time / 3600) % 24);
            },
            "TimeFeatureMapping");

        var amountMapping = _mlContext.Transforms.CustomMapping<CreditCardMLData, AmountFeatures>(
            (input, output) =>
            {
                output.Amount = input.Amount;
                output.Amount_normalized = (float)((input.Amount - _configuration.MinAmount) / (_configuration.MaxAmount - _configuration.MinAmount));
                output.Amount_log = (float)Math.Log(input.Amount + 1);
                output.LogAmount = output.Amount_log;
            },
            "AmountFeatureMapping");

        // 1. Feature hazırlama: Oluşturulan sütunları birleştirin.
        // PCA için kullanacağınız sütun isimlerini (_configuration.FeatureColumns) custom mapping adımlarında oluşturduğunuz sütun isimleriyle uyumlu hale getirin.
        var featurePipeline = timeMapping
            .Append(amountMapping)
            .Append(_mlContext.Transforms.Concatenate("Features", _configuration.FeatureColumns.ToArray()));

        // 2. Normalizasyon: "Features" sütunu üzerinde normalize uygulayın.
        var normalizationPipeline = _mlContext.Transforms.NormalizeMeanVariance("Features");

        // 3. PCA dönüşümü: Normalize edilmiş özellikleri daha düşük boyuta projekte edin.
        var pcaPipeline = _mlContext.Transforms.ProjectToPrincipalComponents(
            outputColumnName: "PCAFeatures",
            inputColumnName: "Features",
            rank: _configuration.ComponentCount,
            exampleWeightColumnName: null);

        // 4. Anomali Tespiti: PCAFeatures’e dayalı anomali skorunu hesaplamak için custom mapping kullanın.
        var anomalyPipeline = _mlContext.Transforms.CustomMapping<PCAPredictionInput, PCAPredictionOutput>(
            (input, output) =>
            {
                if (input.PCAFeatures != null)
                {
                    output.AnomalyScore = (float)Math.Sqrt(input.PCAFeatures.Sum(x => x * x));
                    output.IsAnomaly = output.AnomalyScore > _configuration.AnomalyThreshold;
                    output.Probability = 1.0f / (1.0f + (float)Math.Exp(-output.AnomalyScore));
                    output.PredictedLabel = output.IsAnomaly;
                    output.Score = output.AnomalyScore;
                }
                else
                {
                    output.AnomalyScore = 0;
                    output.IsAnomaly = false;
                    output.Probability = 0;
                    output.PredictedLabel = false;
                    output.Score = 0;
                }
            },
            contractName: "AnomalyScoring");

        // Tüm adımları birleştirin.
        var completePipeline = featurePipeline
            .Append(normalizationPipeline)
            .Append(pcaPipeline)
            .Append(anomalyPipeline);
   

        _logger.LogInformation("PCA pipeline built successfully");
        return completePipeline;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error building PCA pipeline");
        throw;
    }
}

    private float CalculateAnomalyScore(float[] pcaFeatures)
    {
        // Mahalanobis distance calculation
        return (float)Math.Sqrt(pcaFeatures.Select(x => x * x).Sum());
    }

    public ITransformer Train(IDataView trainingData)
    {
        try
        {
            var pipeline = BuildPipeline();
            return pipeline.Fit(trainingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training PCA model");
            throw;
        }
    }
    public void SaveModel(ITransformer model, string modelPath)
    {
        throw new NotImplementedException();
    }

    public ITransformer LoadModel(string modelPath)
    {
        throw new NotImplementedException();
    }

    private DataStatistics GetDataStatistics(IDataView data)
    {
        var statistics = new DataStatistics();

        // Her kolon için istatistikleri hesapla
        foreach (var column in data.Schema)
        {
            if (column.Type is NumberDataViewType)
            {
                var stats = _mlContext.Data.CreateEnumerable<CreditCardModelData>(data, reuseRowObject: false)
                    .Select(x => GetPropertyValue(x, column.Name))
                    .Where(x => !float.IsNaN(x))
                    .ToList();

                if (stats.Any())
                {
                    statistics.ColumnStatistics[column.Name] = new ColumnStatistics
                    {
                        Mean = stats.Average(),
                        StdDev = (float)CalculateStdDev(stats),
                        Min = stats.Min(),
                        Max = stats.Max(),
                        MissingCount = stats.Count(float.IsNaN),
                        NonZeroCount = stats.Count(x => x != 0)
                    };
                }
            }
        }

        return statistics;
    }

    private void LogDataStatistics(DataStatistics statistics)
    {
        foreach (var (column, stats) in statistics.ColumnStatistics)
        {
            _logger.LogInformation(
                "Column {Column} stats - Mean: {Mean:F2}, StdDev: {StdDev:F2}, " +
                "Range: [{Min:F2}, {Max:F2}], Missing: {Missing}, NonZero: {NonZero}",
                column, stats.Mean, stats.StdDev, stats.Min, stats.Max,
                stats.MissingCount, stats.NonZeroCount);
        }
    }

    private static float GetPropertyValue(CreditCardModelData data, string propertyName)
    {
        return (float)data.GetType().GetProperty(propertyName)?.GetValue(data, null);
    }

    private static double CalculateStdDev(IEnumerable<float> values)
    {
        var enumerable = values as float[] ?? values.ToArray();
        var avg = enumerable.Average();
        return Math.Sqrt(enumerable.Average(v => Math.Pow(v - avg, 2)));
    }
}
