using System.Text.Json;
using Analiz.Application.Exceptions;
using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Models.Ensemble;

using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
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

public class ModelService : IModelService
{
    private readonly IModelRepository _modelRepository;

    private readonly IModelEvaluator _modelEvaluator;

    // private readonly IFeatureExtractionService _featureExtractor;
    private readonly MLContext _mlContext;
    private readonly ILogger<ModelService> _logger;
    private readonly IPublisher _publisher;
    private readonly IFeatureExtractionService _featureExtractor;

    public ModelService(
        IModelRepository modelRepository,
        IModelEvaluator modelEvaluator,
        IFeatureExtractionService featureExtractor,
        MLContext mlContext,
        IFeatureExtractionService featureExtractionService,
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

            var modelMetadata = ModelMetadata.Create(
                request.ModelName,
                GenerateVersion(),
                request.ModelType,
                request.Configuration);

            IDataView trainingData, validationData;
            IEstimator<ITransformer> pipeline;
            IModelBuilder modelBuilder;

            switch (request.ModelType)
            {
                case ModelType.LightGBM:
                    var lightGbmConfig = JsonSerializer.Deserialize<LightGBMConfiguration>(request.Configuration);
                    modelBuilder = new LightGBMModelBuilder(_mlContext, lightGbmConfig);
                    (trainingData, validationData) = await PrepareModelData(request);
                    pipeline = modelBuilder.BuildPipeline();
                    break;

                case ModelType.PCA:
                    var pcaConfig = JsonSerializer.Deserialize<PCAConfiguration>(request.Configuration);
                    modelBuilder = new PCAModelBuilder(_mlContext, pcaConfig,_logger);
                    (trainingData, validationData) = await PrepareModelData(request);
                    pipeline = modelBuilder.BuildPipeline();
                    break;

                default:
                    throw new NotSupportedException($"Model type {request.ModelType} not supported");
            }

            _logger.LogInformation("Training model pipeline for {ModelType}", request.ModelType);
            var model = pipeline.Fit(trainingData);

            var metrics = await _modelEvaluator.EvaluateAsync(model, validationData);
            modelMetadata.UpdateMetrics(metrics.ToDictionary());

            await _modelRepository.SaveModelAsync(modelMetadata, model);

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
    
     public async Task<TrainingResult> TrainEnsembleModelAsync(TrainingRequest request)
        {
            try
            {
                _logger.LogInformation("Starting ensemble model training");

                // Eğitim verisini hazırla
                var (trainingData, validationData) = await PrepareModelData(request);

                // Konfigürasyonları deserialize edelim
                var lightGbmConfig = JsonSerializer.Deserialize<LightGBMConfiguration>(request.Configuration,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var pcaConfig = JsonSerializer.Deserialize<PCAConfiguration>(request.Configuration,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // LightGBM ve PCA modellerini ayrı ayrı oluşturun
                var lightGbmBuilder = new LightGBMModelBuilder(_mlContext, lightGbmConfig);
                var pcaBuilder = new PCAModelBuilder(_mlContext, pcaConfig, _logger);

                var lightGbmPipeline = lightGbmBuilder.BuildPipeline();
                var pcaPipeline = pcaBuilder.BuildPipeline();

                _logger.LogInformation("Training LightGBM model...");
                var lightGbmModel = lightGbmPipeline.Fit(trainingData);
                _logger.LogInformation("Training PCA model...");
                var pcaModel = pcaPipeline.Fit(trainingData);

                // Opsiyonel: Her iki modeli ayrı ayrı değerlendirin
                var lightGbmMetrics = await _modelEvaluator.EvaluateAsync(lightGbmModel, validationData);
                var pcaMetrics = await _modelEvaluator.EvaluateAsync(pcaModel, validationData);
                
                _logger.LogInformation("LightGBM AUC: {AUC:F4}", lightGbmMetrics.AUC);
                _logger.LogInformation("PCA AUC: {AUC:F4}", pcaMetrics.AUC);

                // Ensemble model nesnesini oluşturun
                var ensembleModel = new EnsembleModel(_mlContext, lightGbmModel, pcaModel);

                // Ensemble model ile validation seti üzerinde ensemble tahminlerini alın ve ensemble performansını ölçün.
                // Burada basit bir örnek olarak; validation setindeki her kayda ait ensemble tahminini alıp, basit metrik hesaplayabilirsiniz.
                int total = 0, correct = 0;
                var predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, Ensemble.LightGBMPrediction>(lightGbmModel); // Örneğin LightGBM üzerinden de örnek alıyoruz.
                // Diyelim ki, ModelInput veriniz mevcut. (Giriş verisi, Feature extraction sonrası hazır olmalı)
                // Ensemble tahminlerinin performansını ölçmek için bu kısmı genişletebilirsiniz.
                // …

                // Model metadata ve kaydetme işlemleri (her modelin ayrı ayrı kaydedilmesi de düşünülebilir)
                var modelMetadata = ModelMetadata.Create(
                    "Ensemble_" + request.ModelName,
                    GenerateVersion(),
                    ModelType.PCA, // Ensemble modeli için özel bir tür belirleyebilirsiniz.
                    request.Configuration);

                await _modelRepository.SaveModelAsync(modelMetadata, ensembleModel.LightGBMModel); // Örnek: ensemble model metadata'sını kaydedin.
                await _publisher.Publish(new ModelTrainingCompletedEvent(modelMetadata.Id, modelMetadata.ModelName, lightGbmMetrics.ToDictionary()));

                return new TrainingResult
                {
                    ModelId = modelMetadata.Id,
                    Metrics = lightGbmMetrics, // Veya ensemble metrikleri
                    TrainingTime = DateTime.UtcNow - modelMetadata.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ensemble model training for {ModelName}", request.ModelName);
                throw new ModelTrainingException($"Error training ensemble model: {ex.Message}", ex);
            }
        }


    private async Task<(IDataView Training, IDataView Validation)> PrepareLightGBMData(TrainingRequest request)
    {
        _logger.LogInformation("Preparing LightGBM data");

        try
        {
            // Feature extraction kullanarak özellikleri çıkar
            var trainingFeatures =
                await _featureExtractor.ExtractBatchFeaturesAsync(request.TrainingData, request.ModelType);
            var validationFeatures =
                await _featureExtractor.ExtractBatchFeaturesAsync(request.ValidationData, request.ModelType);

            // Training data için ML model datası oluştur
            var trainingData = request.TrainingData.Zip(trainingFeatures, (transaction, features) =>
                new CreditCardModelData
                {
                    Time = features["Time"],
                    Amount = features["Amount"],
                    Label = transaction.IsFraudulent,
                    V1 = features["V1"],
                    V2 = features["V2"],
                    V3 = features["V3"],
                    V4 = features["V4"],
                    V5 = features["V5"],
                    V6 = features["V6"],
                    V7 = features["V7"],
                    V8 = features["V8"],
                    V9 = features["V9"],
                    V10 = features["V10"],
                    V11 = features["V11"],
                    V12 = features["V12"],
                    V13 = features["V13"],
                    V14 = features["V14"],
                    V15 = features["V15"],
                    V16 = features["V16"],
                    V17 = features["V17"],
                    V18 = features["V18"],
                    V19 = features["V19"],
                    V20 = features["V20"],
                    V21 = features["V21"],
                    V22 = features["V22"],
                    V23 = features["V23"],
                    V24 = features["V24"],
                    V25 = features["V25"],
                    V26 = features["V26"],
                    V27 = features["V27"],
                    V28 = features["V28"]
                }).ToList();

            // Validation data için ML model datası oluştur
            var validationData = request.ValidationData.Zip(validationFeatures, (transaction, features) =>
                new CreditCardModelData
                {
                    Time = features["Time"],
                    Amount = features["Amount"],
                    Label = transaction.IsFraudulent,
                    V1 = features["V1"],
                    V2 = features["V2"],
                    V3 = features["V3"],
                    V4 = features["V4"],
                    V5 = features["V5"],
                    V6 = features["V6"],
                    V7 = features["V7"],
                    V8 = features["V8"],
                    V9 = features["V9"],
                    V10 = features["V10"],
                    V11 = features["V11"],
                    V12 = features["V12"],
                    V13 = features["V13"],
                    V14 = features["V14"],
                    V15 = features["V15"],
                    V16 = features["V16"],
                    V17 = features["V17"],
                    V18 = features["V18"],
                    V19 = features["V19"],
                    V20 = features["V20"],
                    V21 = features["V21"],
                    V22 = features["V22"],
                    V23 = features["V23"],
                    V24 = features["V24"],
                    V25 = features["V25"],
                    V26 = features["V26"],
                    V27 = features["V27"],
                    V28 = features["V28"]
                }).ToList();

            _logger.LogInformation(
                $"Converted {trainingData.Count} training records and {validationData.Count} validation records");

            // ML.NET DataView oluşturma
            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            var validationDataView = _mlContext.Data.LoadFromEnumerable(validationData);

            return (trainingDataView, validationDataView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing LightGBM data");
            throw new ModelPreparationException("Error preparing LightGBM data", ex);
        }
    }


    private IEstimator<ITransformer> BuildLightGBMPipeline(string configuration)
    {
        var config = JsonSerializer.Deserialize<LightGBMConfiguration>(configuration);
        return _mlContext.Transforms
            .Conversion.MapValueToKey("Label")
            .Append(_mlContext.Transforms.Concatenate("Features", config.FeatureColumns.ToArray()))
            .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: config.NumberOfLeaves,
                numberOfIterations: config.NumberOfTrees,
                minimumExampleCountPerLeaf: config.MinDataInLeaf,
                learningRate: (float)config.LearningRate));
    }


    private IEstimator<ITransformer> BuildPCAPipeline(string configuration)
    {
        var config = JsonSerializer.Deserialize<PCAConfiguration>(configuration);
        return _mlContext.Transforms
            .Concatenate("Features", config.FeatureColumns.ToArray())
            .Append(_mlContext.Transforms.NormalizeMinMax("NormalizedFeatures", "Features"))
            .Append(_mlContext.Transforms.ProjectToPrincipalComponents(
                outputColumnName: "PCAFeatures",
                inputColumnName: "NormalizedFeatures",
                rank: config.ComponentCount));
    }

    private IDataView CreateLightGBMDataView(List<Dictionary<string, double>> features, IEnumerable<bool> labels)
    {
        var data = features.Zip(labels, (f, l) => new
        {
            Features = f.Values.ToArray(),
            Label = l
        });

        return _mlContext.Data.LoadFromEnumerable(data);
    }


    public async Task<EvaluationResult> EvaluateModelAsync(EvaluationRequest request)
    {
        try
        {
            var modelMetadata = await _modelRepository.GetModelAsync(request.ModelName, request.Version);
            if (modelMetadata == null)
                throw new ModelNotFoundException(request.ModelName, request.Version);

            var model = await LoadModelTransformer(modelMetadata);

            // EvaluationRequest için data hazırlama
            var evaluationData = await PrepareEvaluationData(request);

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

    private async Task<IDataView> PrepareEvaluationData(EvaluationRequest request)
    {
        try
        {
            _logger.LogInformation($"Preparing evaluation data for model {request.ModelName}");

            // Feature extraction
            var features = await _featureExtractor.ExtractBatchFeaturesAsync(request.EvaluationData, request.ModelType);

            // Data dönüşümü
            var evalData = request.EvaluationData.Zip(features, (transaction, feature) =>
                new CreditCardModelData
                {
                    Time = feature["Time"],
                    Amount = feature["Amount"],
                    Label = transaction.IsFraudulent,
                    V1 = feature["V1"],
                    V2 = feature["V2"],
                    V3 = feature["V3"],
                    V4 = feature["V4"],
                    V5 = feature["V5"],
                    V6 = feature["V6"],
                    V7 = feature["V7"],
                    V8 = feature["V8"],
                    V9 = feature["V9"],
                    V10 = feature["V10"],
                    V11 = feature["V11"],
                    V12 = feature["V12"],
                    V13 = feature["V13"],
                    V14 = feature["V14"],
                    V15 = feature["V15"],
                    V16 = feature["V16"],
                    V17 = feature["V17"],
                    V18 = feature["V18"],
                    V19 = feature["V19"],
                    V20 = feature["V20"],
                    V21 = feature["V21"],
                    V22 = feature["V22"],
                    V23 = feature["V23"],
                    V24 = feature["V24"],
                    V25 = feature["V25"],
                    V26 = feature["V26"],
                    V27 = feature["V27"],
                    V28 = feature["V28"]
                }).ToList();

            _logger.LogInformation($"Created {evalData.Count} evaluation records");

            // ML.NET DataView oluştur
            return _mlContext.Data.LoadFromEnumerable(evalData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing evaluation data");
            throw new ModelPreparationException("Error preparing evaluation data", ex);
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
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),_logger),

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


   /* private async Task<(IDataView Training, IDataView Validation)> PrepareModelData(TrainingRequest request)
    {
        _logger.LogInformation($"Preparing {request.ModelType} data");

        try
        {
         //   var trainingFeatures =
           //    await _featureExtractor.ExtractBatchFeaturesAsync(request.TrainingData, request.ModelType);
           // var validationFeatures =
             //   await _featureExtractor.ExtractBatchFeaturesAsync(request.ValidationData, request.ModelType);

            // Data dönüşümü için yardımcı metod
            var convertToModelData = (List<TransactionData> transactions, List<Dictionary<string, float>> features) =>
                transactions.Zip(features, (transaction, feature) =>
                    new CreditCardModelData
                    {
                        Time = feature["Time"],
                        Amount = feature["Amount"],
                        Label = transaction.IsFraudulent,
                        V1 = feature["V1"],
                        V2 = feature["V2"],
                        V3 = feature["V3"],
                        V4 = feature["V4"],
                        V5 = feature["V5"],
                        V6 = feature["V6"],
                        V7 = feature["V7"],
                        V8 = feature["V8"],
                        V9 = feature["V9"],
                        V10 = feature["V10"],
                        V11 = feature["V11"],
                        V12 = feature["V12"],
                        V13 = feature["V13"],
                        V14 = feature["V14"],
                        V15 = feature["V15"],
                        V16 = feature["V16"],
                        V17 = feature["V17"],
                        V18 = feature["V18"],
                        V19 = feature["V19"],
                        V20 = feature["V20"],
                        V21 = feature["V21"],
                        V22 = feature["V22"],
                        V23 = feature["V23"],
                        V24 = feature["V24"],
                        V25 = feature["V25"],
                        V26 = feature["V26"],
                        V27 = feature["V27"],
                        V28 = feature["V28"]
                    }).ToList();

            //var trainingData = convertToModelData(request.TrainingData, trainingFeatures);
            //var validationData = convertToModelData(request.ValidationData, validationFeatures);

            _logger.LogInformation(
                $"Converted {trainingData.Count} training records and {validationData.Count} validation records");

            // ML.NET DataView oluşturma
            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            var validationDataView = _mlContext.Data.LoadFromEnumerable(validationData);

            return (trainingDataView, validationDataView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error preparing {request.ModelType} data");
            throw new ModelPreparationException($"Error preparing {request.ModelType} data", ex);
        }
    }*/
    /*private async Task<(IDataView Training, IDataView Validation)> PrepareModelData(TrainingRequest request)
    {
        _logger.LogInformation($"Preparing {request.ModelType} data");

        try
        {
            _logger.LogDebug($"Training data count: {request.TrainingData?.Count}");

            var trainingData = request.TrainingData.Select(t =>
            {
                var modelData = new CreditCardModelData
                {
                    Amount = Convert.ToSingle(t.Amount),
                    Label = t.IsFraudulent
                };

                // Time özelliği için güvenli dönüşüm
                if (t.AdditionalData != null && t.AdditionalData.TryGetValue("Time", out var timeStr))
                {
                    modelData.Time = Convert.ToSingle(timeStr);
                }
                else
                {
                    modelData.Time = 0f;
                }

                // V özellikleri için güvenli dönüşüm
                for (int i = 1; i <= 28; i++)
                {
                    var key = $"V{i}";
                    var propertyInfo = typeof(CreditCardModelData).GetProperty(key);

                    if (t.AdditionalData != null && t.AdditionalData.TryGetValue(key, out var value))
                    {
                        propertyInfo?.SetValue(modelData, Convert.ToSingle(value));
                    }
                    else
                    {
                        propertyInfo?.SetValue(modelData, 0f);
                    }
                }

                return modelData;
            }).ToList();

            // Aynı işlem validation data için
            var validationData = request.ValidationData.Select(t =>
            {
                var modelData = new CreditCardModelData
                {
                    Amount = Convert.ToSingle(t.Amount),
                    Label = t.IsFraudulent
                };

                if (t.AdditionalData != null && t.AdditionalData.TryGetValue("Time", out var timeStr))
                {
                    modelData.Time = Convert.ToSingle(timeStr);
                }
                else
                {
                    modelData.Time = 0f;
                }

                for (int i = 1; i <= 28; i++)
                {
                    var key = $"V{i}";
                    var propertyInfo = typeof(CreditCardModelData).GetProperty(key);

                    if (t.AdditionalData != null && t.AdditionalData.TryGetValue(key, out var value))
                    {
                        propertyInfo?.SetValue(modelData, Convert.ToSingle(value));
                    }
                    else
                    {
                        propertyInfo?.SetValue(modelData, 0f);
                    }
                }

                return modelData;
            }).ToList();

            _logger.LogInformation(
                $"Converted {trainingData.Count} training records and {validationData.Count} validation records");

            var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            var validationDataView = _mlContext.Data.LoadFromEnumerable(validationData);

            return (trainingDataView, validationDataView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error preparing {request.ModelType} data");
            throw new ModelPreparationException($"Error preparing {request.ModelType} data", ex);
        }
    }
*/
    private async Task<(IDataView Training, IDataView Validation)> PrepareModelData(TrainingRequest request)
{
    try 
    {
        _logger.LogInformation($"Preparing {request.ModelType} data");

        // Training data dönüşümü
        var trainingData = request.TrainingData.Select(t => new CreditCardModelData
        {
            Amount = Convert.ToSingle(t.Amount),
            Label = t.IsFraudulent,
            Time = GetNumericValue(t.AdditionalData, "Time"),
            V1 = GetNumericValue(t.AdditionalData, "V1"),
            V2 = GetNumericValue(t.AdditionalData, "V2"),
            V3 = GetNumericValue(t.AdditionalData, "V3"),
            V4 = GetNumericValue(t.AdditionalData, "V4"),
            V5 = GetNumericValue(t.AdditionalData, "V5"),
            V6 = GetNumericValue(t.AdditionalData, "V6"),
            V7 = GetNumericValue(t.AdditionalData, "V7"),
            V8 = GetNumericValue(t.AdditionalData, "V8"),
            V9 = GetNumericValue(t.AdditionalData, "V9"),
            V10 = GetNumericValue(t.AdditionalData, "V10"),
            V11 = GetNumericValue(t.AdditionalData, "V11"),
            V12 = GetNumericValue(t.AdditionalData, "V12"),
            V13 = GetNumericValue(t.AdditionalData, "V13"),
            V14 = GetNumericValue(t.AdditionalData, "V14"),
            V15 = GetNumericValue(t.AdditionalData, "V15"),
            V16 = GetNumericValue(t.AdditionalData, "V16"),
            V17 = GetNumericValue(t.AdditionalData, "V17"),
            V18 = GetNumericValue(t.AdditionalData, "V18"),
            V19 = GetNumericValue(t.AdditionalData, "V19"),
            V20 = GetNumericValue(t.AdditionalData, "V20"),
            V21 = GetNumericValue(t.AdditionalData, "V21"),
            V22 = GetNumericValue(t.AdditionalData, "V22"),
            V23 = GetNumericValue(t.AdditionalData, "V23"),
            V24 = GetNumericValue(t.AdditionalData, "V24"),
            V25 = GetNumericValue(t.AdditionalData, "V25"),
            V26 = GetNumericValue(t.AdditionalData, "V26"),
            V27 = GetNumericValue(t.AdditionalData, "V27"),
            V28 = GetNumericValue(t.AdditionalData, "V28")
        }).ToList();

        // Veri seti istatistiklerini logla
        var trainFraudCount = trainingData.Count(x => x.Label);
        _logger.LogInformation("Training data stats - Total: {Total}, Fraud: {Fraud}, Ratio: {Ratio:P2}", 
            trainingData.Count, trainFraudCount, (double)trainFraudCount/trainingData.Count);

        var validationData = request.ValidationData.Select(t => new CreditCardModelData
        {
            Amount = Convert.ToSingle(t.Amount),
            Label = t.IsFraudulent,
            Time = GetNumericValue(t.AdditionalData, "Time"),
            V1 = GetNumericValue(t.AdditionalData, "V1"),
            V2 = GetNumericValue(t.AdditionalData, "V2"),
            V3 = GetNumericValue(t.AdditionalData, "V3"),
            V4 = GetNumericValue(t.AdditionalData, "V4"),
            V5 = GetNumericValue(t.AdditionalData, "V5"),
            V6 = GetNumericValue(t.AdditionalData, "V6"),
            V7 = GetNumericValue(t.AdditionalData, "V7"),
            V8 = GetNumericValue(t.AdditionalData, "V8"),
            V9 = GetNumericValue(t.AdditionalData, "V9"),
            V10 = GetNumericValue(t.AdditionalData, "V10"),
            V11 = GetNumericValue(t.AdditionalData, "V11"),
            V12 = GetNumericValue(t.AdditionalData, "V12"),
            V13 = GetNumericValue(t.AdditionalData, "V13"),
            V14 = GetNumericValue(t.AdditionalData, "V14"),
            V15 = GetNumericValue(t.AdditionalData, "V15"),
            V16 = GetNumericValue(t.AdditionalData, "V16"),
            V17 = GetNumericValue(t.AdditionalData, "V17"),
            V18 = GetNumericValue(t.AdditionalData, "V18"),
            V19 = GetNumericValue(t.AdditionalData, "V19"),
            V20 = GetNumericValue(t.AdditionalData, "V20"),
            V21 = GetNumericValue(t.AdditionalData, "V21"),
            V22 = GetNumericValue(t.AdditionalData, "V22"),
            V23 = GetNumericValue(t.AdditionalData, "V23"),
            V24 = GetNumericValue(t.AdditionalData, "V24"),
            V25 = GetNumericValue(t.AdditionalData, "V25"),
            V26 = GetNumericValue(t.AdditionalData, "V26"),
            V27 = GetNumericValue(t.AdditionalData, "V27"),
            V28 = GetNumericValue(t.AdditionalData, "V28")
        }).ToList();

        // Validation seti istatistiklerini logla
        var validFraudCount = validationData.Count(x => x.Label);
        _logger.LogInformation("Validation data stats - Total: {Total}, Fraud: {Fraud}, Ratio: {Ratio:P2}", 
            validationData.Count, validFraudCount, (double)validFraudCount/validationData.Count);

        // Veri setlerini kontrol et
        ValidateDatasets(trainingData, validationData);

        _logger.LogInformation(
            $"Converted {trainingData.Count} training records and {validationData.Count} validation records");

        var trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);
        var validationDataView = _mlContext.Data.LoadFromEnumerable(validationData);
        var normalizations = Enumerable.Range(1, 28)
            .Select(i => new InputOutputColumnPair($"V{i}_normalized", $"V{i}"))
            .ToArray();

        // Feature engineering pipeline'ı oluştur
        var pipeline = _mlContext.Transforms
            // Önce Amount normalizasyonu
            .NormalizeMinMax("Amount_normalized", nameof(CreditCardModelData.Amount))
            // Zaman özelliklerini dönüştür
            .Append(_mlContext.Transforms.CustomMapping(
                (CreditCardMLData input, TimeFeatures output) =>
                {
                    const double daySeconds = 24 * 60 * 60;
                    output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / daySeconds);
                    output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / daySeconds);
                },
                "TimeFeatureMapping"))
            // V özelliklerini normalize et
            .Append(_mlContext.Transforms.NormalizeMeanVariance(normalizations))
            // Tüm özellikleri birleştir
            .Append(_mlContext.Transforms.Concatenate("Features",
                new[] { "Amount_normalized", "TimeSin", "TimeCos" }
                    .Concat(normalizations.Select(n => n.OutputColumnName))
                    .ToArray()
            ));

        // Pipeline'ı uygula
        _logger.LogInformation("Applying feature engineering pipeline...");
        var transformedTraining = pipeline.Fit(trainingDataView).Transform(trainingDataView);
        var transformedValidation = pipeline.Fit(validationDataView).Transform(validationDataView);

        return (transformedTraining, transformedValidation);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error preparing model data for {ModelType}", request.ModelType);
        throw new ModelPreparationException($"Error preparing data for {request.ModelType}", ex);
    }
}
    private void ValidateDatasets(List<CreditCardModelData> training, List<CreditCardModelData> validation)
    {
        if (training == null || !training.Any())
            throw new ArgumentException("Training data cannot be empty");
        
        if (validation == null || !validation.Any())
            throw new ArgumentException("Validation data cannot be empty");

        var trainFraud = training.Count(x => x.Label);
        if (trainFraud == 0)
            throw new InvalidOperationException("Training data must contain fraud cases");

        var validFraud = validation.Count(x => x.Label);
        if (validFraud == 0)
            throw new InvalidOperationException("Validation data must contain fraud cases");

        _logger.LogInformation("Data validation passed - Training fraud ratio: {TrainRatio:P2}, Validation fraud ratio: {ValidRatio:P2}",
            (double)trainFraud/training.Count, 
            (double)validFraud/validation.Count);
    }

    private static float GetNumericValue(Dictionary<string, string> data, string key)
    {
        if (data != null && data.TryGetValue(key, out var value))
        {
            return float.TryParse(value, out var result) ? result : 0f;
        }
        return 0f;
    }
    private IEstimator<ITransformer> BuildFeatureEngineeringPipeline()
    {
        var pipeline = _mlContext.Transforms
            // Normalizasyon
            .NormalizeMinMax("Amount_normalized", "Amount")
            // Zaman bazlı özellikler
            .Append(_mlContext.Transforms.CustomMapping(
                (CreditCardMLData input, TimeFeatures output) =>
                {
                    const double daySeconds = 24 * 60 * 60;
                    output.TimeSin = (float)Math.Sin(2 * Math.PI * input.Time / daySeconds);
                    output.TimeCos = (float)Math.Cos(2 * Math.PI * input.Time / daySeconds);
                },
                "TimeFeatureMapping"))
            // V özellikleri normalizasyonu
            .Append(_mlContext.Transforms.Concatenate(
                "NormalizedFeatures",
                "Amount_normalized",
                "TimeSin",
                "TimeCos",
                "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
                "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
                "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"));

        return pipeline;
    }
 

    private void ValidateClassDistribution(List<CreditCardModelData> data, string datasetName)
    {
        var fraudCount = data.Count(x => x.Label);
        var totalCount = data.Count;
        var fraudRatio = (double)fraudCount / totalCount;

        _logger.LogInformation(
            "{DatasetName} data distribution - Total: {Total}, Fraud: {Fraud}, Ratio: {Ratio:P2}", 
            datasetName, totalCount, fraudCount, fraudRatio);

        if (fraudCount == 0)
            throw new InvalidOperationException($"No fraud cases in {datasetName} dataset");

        if (fraudRatio < 0.0001)
            _logger.LogWarning("Very low fraud ratio in {DatasetName} dataset: {Ratio:P4}", 
                datasetName, fraudRatio);
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