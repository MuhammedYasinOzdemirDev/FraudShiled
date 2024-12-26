using Microsoft.ML;

namespace Analiz.Application.Interfaces;

public interface IModelPredictionService
{
    Task<ITransformer> GetModelTransformerAsync(string modelName);
}