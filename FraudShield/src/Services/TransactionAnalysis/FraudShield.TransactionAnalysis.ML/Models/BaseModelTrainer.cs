using FraudShield.TransactionAnalysis.Application.Interfaces.ML;
using FraudShield.TransactionAnalysis.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.ML.Models;


public abstract class BaseModelTrainer
{
    protected readonly MLContext MlContext;
    protected readonly ILogger _logger;

    protected BaseModelTrainer(MLContext mlContext, ILogger logger)
    {
        MlContext = mlContext;
        _logger = logger;
    }

    protected virtual IEstimator<ITransformer> BuildFeaturizationPipeline(
        IModelConfiguration config)
    {
        var pipeline = MlContext.Transforms
            .Concatenate("Features", config.FeatureColumns.ToArray())
            .Append(MlContext.Transforms.NormalizeMinMax("Features"));

        return pipeline;
    }

    protected virtual async Task<string> SaveModelAsync(
        ITransformer trainedModel, 
        string modelName)
    {
        var modelPath = Path.Combine("Models", $"{modelName}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
        await using var fs = File.Create(modelPath);
        MlContext.Model.Save(trainedModel, null, fs);
        return modelPath;
    }

    protected virtual async Task<ModelMetrics> CalculateMetricsAsync(
        ITransformer model, 
        IDataView testData)
    {
        var predictions = model.Transform(testData);
        var metrics = MlContext.BinaryClassification.Evaluate(predictions);

        return new ModelMetrics
        {
            Accuracy = metrics.Accuracy,
            Precision = metrics.PositivePrecision,
            Recall = metrics.PositiveRecall,
            F1Score = metrics.F1Score,
            AUC = metrics.AreaUnderRocCurve,
            CalculatedAt = DateTime.UtcNow,
            CustomMetrics = new Dictionary<string, double>()
        };
    }
}