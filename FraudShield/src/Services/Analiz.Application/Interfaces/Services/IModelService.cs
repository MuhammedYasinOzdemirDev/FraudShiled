using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.Evaluation;
using Microsoft.ML;

namespace Analiz.Application.Interfaces;

public interface IModelService
{
    Task<TrainingResult> TrainModelAsync(TrainingRequest request);
    Task<TrainingResult> TrainEnsembleModelAsync(TrainingRequest request);
    Task<EvaluationResult> EvaluateModelAsync(EvaluationRequest request);
    Task<ModelMetrics> GetModelMetricsAsync(string modelName);
    Task<bool> UpdateModelAsync(string modelName, ModelUpdateRequest request);
    Task<List<ModelVersion>> GetModelVersionsAsync(string modelName);
    Task<bool> ActivateModelVersionAsync(string modelName, string version);
    Task<ITransformer> GetModelTransformerAsync(string modelName);
    Task<ModelPrediction> PredictAsync(string modelName, ModelInput input);
}
