using Analiz.Application.Exceptions;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities.ML;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Analiz.ML.Evaluator;

public class ModelEvaluator : IModelEvaluator
{
    private readonly MLContext _mlContext;
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<ModelEvaluator> _logger;

    public ModelEvaluator(
        MLContext mlContext,
        IModelRepository modelRepository,
        ILogger<ModelEvaluator> logger)
    {
        _mlContext = mlContext;
        _modelRepository = modelRepository;
        _logger = logger;
    }

    public async Task<ModelMetrics> EvaluateAsync(ITransformer model, IDataView evaluationData)
    {
        try
        {
            var predictions = model.Transform(evaluationData);
            var metrics = _mlContext.BinaryClassification.Evaluate(predictions);

            var confusionMatrix = new Dictionary<string, double>();

            // Calculate confusion matrix metrics from predictions
            var predictionsEnumerable = _mlContext.Data
                .CreateEnumerable<PredictionResult>(predictions, reuseRowObject: false);

            var results = predictionsEnumerable.ToList();
            var actualLabels = results.Select(p => p.Label).ToList();
            var predictedLabels = results.Select(p => p.PredictedLabel).ToList();

            var truePositives = actualLabels.Zip(predictedLabels, (a, p) => a && p).Count(x => x);
            var falsePositives = actualLabels.Zip(predictedLabels, (a, p) => !a && p).Count(x => x);
            var trueNegatives = actualLabels.Zip(predictedLabels, (a, p) => !a && !p).Count(x => x);
            var falseNegatives = actualLabels.Zip(predictedLabels, (a, p) => a && !p).Count(x => x);

            return new ModelMetrics
            {
                Accuracy = metrics.Accuracy,
                Precision = metrics.PositivePrecision,
                Recall = metrics.PositiveRecall,
                F1Score = metrics.F1Score,
                AUC = metrics.AreaUnderRocCurve,
                AdditionalMetrics = new Dictionary<string, double>
                {
                    ["ConfusionMatrix_TruePositives"] = truePositives,
                    ["ConfusionMatrix_FalsePositives"] = falsePositives,
                    ["ConfusionMatrix_TrueNegatives"] = trueNegatives,
                    ["ConfusionMatrix_FalseNegatives"] = falseNegatives,
                    ["AreaUnderPrecisionRecallCurve"] = metrics.AreaUnderPrecisionRecallCurve,
                    ["NegativePrecision"] = metrics.NegativePrecision,
                    ["NegativeRecall"] = metrics.NegativeRecall
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model");
            throw;
        }
    }

    public async Task<Dictionary<string, double>> CalculateMetricsAsync(
        IEnumerable<PredictionResult> predictions,
        IEnumerable<bool> actualLabels)
    {
        var predictionArray = predictions.Select(p => p.PredictedLabel).ToArray();
        var actualArray = actualLabels.ToArray();
        var probabilities = predictions.Select(p => (double)p.Probability).ToArray(); // Cast float to double

        return new Dictionary<string, double>
        {
            ["Accuracy"] = CalculateAccuracy(predictionArray, actualArray),
            ["Precision"] = CalculatePrecision(predictionArray, actualArray),
            ["Recall"] = CalculateRecall(predictionArray, actualArray),
            ["F1Score"] = CalculateF1Score(predictionArray, actualArray),
            ["AUC"] = CalculateAUC(probabilities, actualArray) // probabilities is now double[]
        };
    }
    
    public async Task<PerformanceReport> GeneratePerformanceReportAsync(string modelName)
    {
        var model = await _modelRepository.GetActiveModelAsync(modelName);
        if (model == null)
            throw new ModelNotFoundException(modelName);

        return new PerformanceReport
        {
            ModelName = modelName,
            Version = model.Version,
            CurrentMetrics = new ModelMetrics
            {
                Accuracy = model.Metrics["Accuracy"],
                Precision = model.Metrics["Precision"],
                Recall = model.Metrics["Recall"],
                F1Score = model.Metrics["F1Score"],
                AUC = model.Metrics["AUC"]
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static double CalculateAccuracy(bool[] predicted, bool[] actual)
    {
        if (predicted.Length != actual.Length) throw new ArgumentException("Arrays must be same length");
        return predicted.Zip(actual, (p, a) => p == a).Count(x => x) / (double)predicted.Length;
    }

    private static double CalculatePrecision(bool[] predicted, bool[] actual)
    {
        var truePositives = predicted.Zip(actual, (p, a) => p && a).Count(x => x);
        var falsePositives = predicted.Zip(actual, (p, a) => p && !a).Count(x => x);
        return truePositives / (double)(truePositives + falsePositives);
    }

    private static double CalculateRecall(bool[] predicted, bool[] actual)
    {
        var truePositives = predicted.Zip(actual, (p, a) => p && a).Count(x => x);
        var falseNegatives = predicted.Zip(actual, (p, a) => !p && a).Count(x => x);
        return truePositives / (double)(truePositives + falseNegatives);
    }

    private static double CalculateF1Score(bool[] predicted, bool[] actual)
    {
        var precision = CalculatePrecision(predicted, actual);
        var recall = CalculateRecall(predicted, actual);
        return 2 * (precision * recall) / (precision + recall);
    }

    private static double CalculateAUC(double[] probabilities, bool[] actual)
    {
        // Simple AUC calculation using trapezoidal rule
        var points = probabilities.Zip(actual, (p, a) => new { Probability = p, Actual = a })
            .OrderByDescending(x => x.Probability)
            .ToList();

        double auc = 0;
        int positives = actual.Count(x => x);
        int negatives = actual.Length - positives;
        double tpr = 0, lastTpr = 0;
        double fpr = 0, lastFpr = 0;

        foreach (var point in points)
        {
            if (point.Actual)
                tpr += 1.0 / positives;
            else
                fpr += 1.0 / negatives;

            auc += (fpr - lastFpr) * (tpr + lastTpr) / 2;
            lastFpr = fpr;
            lastTpr = tpr;
        }

        return auc;
    }
}