using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Models;
using FraudShield.TransactionAnalysis.ML.Extensions;
using FraudShield.TransactionAnalysis.ML.Models.Common;
using FraudShield.TransactionAnalysis.ML.Models.Common.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.ML.Models.LightGBM.Prediction;

public class LightGBMModel : ModelBase, ITrainableModel, IPredictableModel
{
    private readonly ITransformer _trainedModel;
    private readonly MLContext _mlContext;
    private readonly ILogger<LightGBMModel> _logger;
    private readonly PredictionEngine<ModelInput, ModelOutput> _predictionEngine;

    private LightGBMModel(
        string name,
        string version,
        MLContext mlContext,
        ITransformer trainedModel,
        ILogger<LightGBMModel> logger)
    {
        Name = name;
        Version = version;
        Type = ModelType.LightGBM;
        _mlContext = mlContext;
        _trainedModel = trainedModel;
        _logger = logger;
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(trainedModel);
    }

    public static LightGBMModel Create(
        string name,
        string version,
        MLContext mlContext,
        ITransformer trainedModel,
        ILogger<LightGBMModel> logger)
    {
        return new LightGBMModel(name, version, mlContext, trainedModel, logger);
    }

    public async Task<Result<IModelBase>> TrainAsync(
        TrainingData trainingData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Status = ModelStatus.Training;

            // Build pipeline
            var pipeline = BuildTrainingPipeline();

            _logger.LogInformation($"Starting training for model {Name}");
            var trainedModel = pipeline.Fit(trainingData.Data);

            var evaluationResult = await EvaluateAsync(trainingData.ValidationData);
            if (evaluationResult.IsFailure)
                return Result<IModelBase>.Failure(evaluationResult.Error);

            Metrics = evaluationResult.Value.ToDictionary();
            Status = ModelStatus.Active;

            return Result<IModelBase>.Success(this);
        }
        catch (Exception ex)
        {
            Status = ModelStatus.Failed;
            _logger.LogError(ex, $"Training failed for model {Name}");
            return Result<IModelBase>.Failure(ex.Message);
        }
    }

    public async Task<Result<ModelMetrics>> EvaluateAsync(EvaluationData evaluationData)
    {
        try
        {
            var mlMetrics = _mlContext.BinaryClassification.Evaluate(_trainedModel.Transform(evaluationData.Data));
            var confusionMatrix = mlMetrics.ConfusionMatrix;
            var truePositives = mlMetrics.ConfusionMatrix.GetCountForClassPair(1, 1);
            var trueNegatives = mlMetrics.ConfusionMatrix.GetCountForClassPair(0, 0);
            var falsePositives = mlMetrics.ConfusionMatrix.GetCountForClassPair(1, 0);
            var falseNegatives = mlMetrics.ConfusionMatrix.GetCountForClassPair(0, 1);

            var modelMetrics = new ModelMetrics
            {
                Accuracy = mlMetrics.Accuracy,
                Precision = mlMetrics.PositivePrecision,
                Recall = mlMetrics.PositiveRecall,
                F1Score = mlMetrics.F1Score,
                AUC = mlMetrics.AreaUnderRocCurve,
                ConfusionMatrix = new ConfusionMatrix
                {
                    TruePositives = truePositives,
                    TrueNegatives = trueNegatives,
                    FalsePositives = falsePositives,
                    FalseNegatives = falseNegatives
                },
                CustomMetrics = evaluationData.Metadata
                    .Where(kvp => double.TryParse(kvp.Value, out _))
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => double.Parse(kvp.Value)
                    )
            };


            return Result<ModelMetrics>.Success(modelMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Evaluation failed for model {Name}");
            return Result<ModelMetrics>.Failure(ex.Message);
        }
    }

    public async Task<Result<PredictionResult>> PredictAsync(PredictionData data)
    {
        try
        {
            var modelInput = MapToModelInput(data);
            var prediction = _predictionEngine.Predict(modelInput);

            return Result<PredictionResult>.Success(new PredictionResult
            {
                Score = prediction.Score,
                Probability = prediction.Probability,
                PredictedLabel = prediction.PredictedLabel,
                PredictedAt = DateTime.UtcNow,
                Features = ExtractFeatureImportance(prediction)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Prediction failed for model {Name}");
            return Result<PredictionResult>.Failure(ex.Message);
        }
    }

    public async Task<Result<IEnumerable<PredictionResult>>> PredictBatchAsync(
        IEnumerable<PredictionData> data)
    {
        try
        {
            var results = new List<PredictionResult>();
            foreach (var item in data)
            {
                var result = await PredictAsync(item);
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
            }

            return Result<IEnumerable<PredictionResult>>.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Batch prediction failed for model {Name}");
            return Result<IEnumerable<PredictionResult>>.Failure(ex.Message);
        }
    }

    private IEstimator<ITransformer> BuildTrainingPipeline()
    {
        var featurePipeline = _mlContext.Transforms
            .Concatenate("Features", new[]
            {
                nameof(ModelInput.Amount),
                nameof(ModelInput.TransactionHour),
                nameof(ModelInput.TransactionDay),
                nameof(ModelInput.MerchantRiskScore)
            })
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"));

        var trainer = _mlContext.BinaryClassification.Trainers.LightGbm(
            labelColumnName: "Label",
            featureColumnName: "Features",
            numberOfLeaves: 31,
            minimumExampleCountPerLeaf: 20,
            learningRate: 0.1,
            numberOfIterations: 100);

        return featurePipeline.Append(trainer);
    }

    private ModelInput MapToModelInput(PredictionData data) =>
        new()
        {
            Amount = data.Amount,
            TransactionHour = data.TransactionTime.Hour,
            TransactionDay = (int)data.TransactionTime.DayOfWeek,
            MerchantRiskScore = data.CustomFeatures.GetValueOrDefault("MerchantRiskScore", 0)
        };

    private Dictionary<string, float> ExtractFeatureImportance(ModelOutput prediction)
    {
        // Feature importance extraction logic
        return new Dictionary<string, float>();
    }
}