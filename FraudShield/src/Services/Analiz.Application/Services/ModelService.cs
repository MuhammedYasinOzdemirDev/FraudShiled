using System.Text.Json;
using Analiz.Application.Exceptions;
using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using Analiz.Domain.Events;
using Analiz.ML.Models.LightGBM;
using Analiz.ML.Models.PCA;
using FraudShield.TransactionAnalysis.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using ModelInput = Analiz.Domain.Entities.ML.ModelInput;
using ModelMetrics = Analiz.Domain.Entities.ML.ModelMetrics;

namespace Analiz.Application.Services;

public class ModelService : IModelService,IModelPredictionService
{
    private readonly IModelRepository _modelRepository;
    private readonly IModelEvaluator _modelEvaluator;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly MLContext _mlContext;
    private readonly ILogger<ModelService> _logger;
    private readonly IPublisher _publisher;

    public ModelService(
        IModelRepository modelRepository,
        IModelEvaluator modelEvaluator,
        IFeatureExtractionService featureExtractor,
        MLContext mlContext,
        ILogger<ModelService> logger,
        IPublisher publisher)
    {
        _modelRepository = modelRepository;
        _modelEvaluator = modelEvaluator;
        _featureExtractor = featureExtractor;
        _mlContext = mlContext;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<TrainingResult> TrainModelAsync(TrainingRequest request)
    {
        try
        {
            _logger.LogInformation("Starting model training for {ModelName}", request.ModelName);

            // Model metadata oluştur
            var modelMetadata = ModelMetadata.Create(
                request.ModelName,
                GenerateVersion(),
                request.ModelType,
                request.Configuration);

            // Model builder'ı seç ve konfigüre et
            var modelBuilder = CreateModelBuilder(request.ModelType, request.Configuration);

            // Training ve validation datayı hazırla
            var trainingData = await PrepareModelData(request.TrainingData, request.Labels);
            var validationData = await PrepareModelData(request.ValidationData, request.Labels);

// Pass IDataView to Train
            var model = modelBuilder.Train(trainingData);


            // Model değerlendirme
            var metrics = await _modelEvaluator.EvaluateAsync(model, validationData);

            // Model metadata güncelle
            modelMetadata.UpdateMetrics(metrics.ToDictionary());

            // Model kaydet
            await _modelRepository.SaveModelAsync(modelMetadata, model);

            // Event yayınla
            await _publisher.Publish(new ModelTrainingCompletedEvent(
                modelMetadata.Id,
                modelMetadata.ModelName,
                metrics.ToDictionary()));

            return new TrainingResult
            {
                ModelId = modelMetadata.Id,
                Metrics = metrics,
                TrainingTime = DateTime.UtcNow - modelMetadata.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model training for {ModelName}", request.ModelName);
            throw new ModelTrainingException($"Error training model: {ex.Message}", ex);
        }
    }

    public async Task<EvaluationResult> EvaluateModelAsync(EvaluationRequest request)
    {
        try
        {
            var modelMetadata = await _modelRepository.GetModelAsync(request.ModelName, request.Version);
            if (modelMetadata == null)
                throw new ModelNotFoundException(request.ModelName, request.Version);

            var model = await LoadModelTransformer(modelMetadata);
            var evaluationData = await PrepareModelData(request.EvaluationData, request.Labels);

            var metrics = await _modelEvaluator.EvaluateAsync(model, evaluationData);

            return new EvaluationResult
            {
                ModelId = modelMetadata.Id,
                Metrics = metrics,
                EvaluationTime = DateTime.UtcNow
            };
        }
        catch (ModelNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model {ModelName}", request.ModelName);
            throw new ModelEvaluationException($"Error evaluating model: {ex.Message}", ex);
        }
    }

    public async Task<ModelMetrics> GetModelMetricsAsync(string modelName)
    {
        try
        {
            var modelMetadata = await _modelRepository.GetActiveModelAsync(modelName);
            if (modelMetadata == null)
                throw new ModelNotFoundException(modelName);

            return new ModelMetrics
            {
                Accuracy = modelMetadata.Metrics["Accuracy"],
                Precision = modelMetadata.Metrics["Precision"],
                Recall = modelMetadata.Metrics["Recall"],
                F1Score = modelMetadata.Metrics["F1Score"],
                AUC = modelMetadata.Metrics["AUC"],
                AdditionalMetrics = modelMetadata.Metrics
            };
        }
        catch (ModelNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model metrics for {ModelName}", modelName);
            throw new ModelMetricsException($"Error getting model metrics: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateModelAsync(string modelName, ModelUpdateRequest request)
    {
        var model = await _modelRepository.GetModelAsync(modelName, request.Version);
        if (model == null)
            return false;

        // Model güncelleme işlemleri
        model.UpdateConfiguration(request.Configuration);
        await _modelRepository.UpdateModelAsync(model);

        await _publisher.Publish(new ModelUpdatedEvent(model.Id, modelName));

        return true;
    }

    public async Task<List<ModelVersion>> GetModelVersionsAsync(string modelName)
    {
        return await _modelRepository.GetModelVersionsAsync(modelName);
    }

    public async Task<bool> ActivateModelVersionAsync(string modelName, string version)
    {
        var model = await _modelRepository.GetModelAsync(modelName, version);
        if (model == null)
            return false;

        // Aktif modeli deaktive et
        var activeModel = await _modelRepository.GetActiveModelAsync(modelName);
        if (activeModel != null)
        {
            activeModel.Deactivate();
            await _modelRepository.UpdateModelAsync(activeModel);
        }

        // Yeni modeli aktive et
        model.Activate();
        await _modelRepository.UpdateModelAsync(model);

        await _publisher.Publish(new ModelActivatedEvent(model.Id, modelName, version));

        return true;
    }

    public async Task<ITransformer> GetModelTransformerAsync(string modelName)
    {
        try
        {
            _logger.LogInformation("Retrieving model transformer for model {ModelName}", modelName);

            var modelMetadata = await _modelRepository.GetActiveModelAsync(modelName);
            if (modelMetadata == null)
            {
                throw new ModelNotFoundException($"Active model not found for {modelName}");
            }

            if (modelMetadata.Status != ModelStatus.Active)
            {
                throw new Exception(
                    $"Model {modelName} is not active. Current status: {modelMetadata.Status}");
            }

            var transformer = await LoadModelTransformer(modelMetadata);
            if (transformer == null)
            {
                throw new ModelLoadException($"Failed to load transformer for model {modelName}");
            }

            _logger.LogInformation("Successfully retrieved transformer for model {ModelName}", modelName);
            return transformer;
        }
        catch (ModelNotFoundException)
        {
            _logger.LogWarning("Model {ModelName} not found", modelName);
            throw;
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving transformer for model {ModelName}", modelName);
            throw new Exception(
                $"Failed to retrieve model transformer for {modelName}", ex);
        }
    }

    public Task<ModelPrediction> PredictAsync(string modelName, ModelInput input)
    {
        throw new NotImplementedException();
    }

    private IModelBuilder CreateModelBuilder(ModelType type, string configuration)
    {
        try
        {
            return type switch
            {
                ModelType.PCA => new PCAModelBuilder(
                    _mlContext,
                    JsonSerializer.Deserialize<PCAConfiguration>(configuration,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })),

                ModelType.LightGBM => new LightGBMModelBuilder(
                    _mlContext,
                    JsonSerializer.Deserialize<LightGBMConfiguration>(configuration,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })),

                _ => throw new NotSupportedException($"Model type {type} is not supported.")
            };
        }
        catch (JsonException ex)
        {
            throw new Exception($"Error parsing configuration for model type {type}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating model builder for type {type}: {ex.Message}", ex);
        }
    }

    private string GenerateVersion() => $"v{DateTime.UtcNow:yyyyMMdd.HHmmss}";

    private IDataView PrepareTrainingData(List<FeatureSet> features, IEnumerable<bool> labels)
    {
        // Training data hazırlama mantığı
        var trainingData = new List<ModelInput>();

        using (var enumerator = labels.GetEnumerator())
        {
            foreach (var feature in features)
            {
                if (!enumerator.MoveNext())
                    break;

                trainingData.Add(new ModelInput
                {
                    Features = feature.ToVector(),
                    Label = enumerator.Current
                });
            }
        }

        return _mlContext.Data.LoadFromEnumerable(trainingData);
    }

    private async Task<IDataView> PrepareModelData(List<TransactionData> data, IEnumerable<bool> labels = null)
    {
        try
        {
            _logger.LogDebug("Starting model data preparation for {Count} transactions", data.Count);

            // Extract features asynchronously
            var features = await _featureExtractor.ExtractBatchFeaturesAsync(data);
            var modelData = new List<ModelInput>();

            if (labels != null)
            {
                using (var labelEnumerator = labels.GetEnumerator())
                {
                    foreach (var feature in features)
                    {
                        if (!labelEnumerator.MoveNext())
                        {
                            _logger.LogWarning("Label count mismatch: fewer labels than features");
                            break;
                        }

                        modelData.Add(new ModelInput
                        {
                            Features = feature.ToVector(),
                            Label = labelEnumerator.Current
                        });
                    }
                }
            }
            else
            {
                modelData.AddRange(features.Select(feature => new ModelInput
                {
                    Features = feature.ToVector()
                }));
            }

            _logger.LogDebug("Completed model data preparation. Generated {Count} model inputs", modelData.Count);
            return _mlContext.Data.LoadFromEnumerable(modelData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing model data");
            throw new ModelPreparationException("Error preparing data for model", ex);
        }
    }

    private async Task<ITransformer> LoadModelTransformer(ModelMetadata modelMetadata)
    {
        try
        {
            return await _modelRepository.LoadModelTransformerAsync(modelMetadata.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model transformer for model {ModelId}", modelMetadata.Id);
            throw new ModelLoadException($"Error loading model transformer: {ex.Message}");
        }
    }
}