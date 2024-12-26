using Analiz.Domain.Entities;
using Microsoft.ML;

namespace Analiz.Application.Interfaces.Repositories;

public interface IModelRepository
{
    Task<ModelMetadata> GetModelAsync(string modelName, string version);
    Task<ModelMetadata> GetActiveModelAsync(string modelName);
    Task<List<ModelVersion>> GetModelVersionsAsync(string modelName);
    Task SaveModelAsync(ModelMetadata metadata, ITransformer model);
    Task UpdateModelAsync(ModelMetadata metadata);
    Task<ModelMetadata> GetLatestModelAsync(string modelName);
    Task DeactivateModelAsync(string modelName, string version);
    
    Task<ITransformer> LoadModelTransformerAsync(Guid modelId);
    Task SaveModelTransformerAsync(Guid modelId, ITransformer model);
}
