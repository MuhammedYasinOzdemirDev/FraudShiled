using Analiz.Application.Exceptions;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Analiz.Persistence.Repositories;

public class ModelRepository : IModelRepository
{
    private readonly ApplicationDbContext _context;
 //   private readonly IFileStorage _fileStorage;
    private readonly ILogger<ModelRepository> _logger;
    private readonly string _modelStoragePath;

    public ModelRepository(
        ApplicationDbContext context,
      //  IFileStorage fileStorage,
        ILogger<ModelRepository> logger,
        IConfiguration configuration)
    {
        _context = context;
       // _fileStorage = fileStorage;
        _logger = logger;
        _modelStoragePath = configuration["ML:ModelStoragePath"] ?? "Models";
    }

    public async Task<ModelMetadata> GetModelAsync(string modelName, string version)
    {
        return await _context.Models
            .FirstOrDefaultAsync(m => m.ModelName == modelName && m.Version == version);
    }

    public async Task<ModelMetadata> GetActiveModelAsync(string modelName)
    {
        return await _context.Models
            .FirstOrDefaultAsync(m => m.ModelName == modelName && m.Status == ModelStatus.Active);
    }

    public async Task<List<ModelVersion>> GetModelVersionsAsync(string modelName)
    {
        var models = await _context.Models
            .Where(m => m.ModelName == modelName)
            .OrderByDescending(m => m.TrainedAt)
            .ToListAsync();

        return models.Select(m => new ModelVersion
        {
            Version = m.Version,
            Status = m.Status,
            TrainedAt = m.TrainedAt,
            Metrics = m.Metrics
        }).ToList();
    }

    public async Task SaveModelAsync(ModelMetadata metadata, ITransformer model)
    {
        // Model dosyasını kaydet
        var modelPath = GetModelPath(metadata.ModelName, metadata.Version);
        await SaveModelFileAsync(model, modelPath);

        // Metadata'yı kaydet
        await _context.Models.AddAsync(metadata);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateModelAsync(ModelMetadata metadata)
    {
        _context.Models.Update(metadata);
        await _context.SaveChangesAsync();
    }

    public async Task<ModelMetadata> GetLatestModelAsync(string modelName)
    {
        return await _context.Models
            .Where(m => m.ModelName == modelName)
            .OrderByDescending(m => m.TrainedAt)
            .FirstOrDefaultAsync();
    }

    public async Task DeactivateModelAsync(string modelName, string version)
    {
        var model = await GetModelAsync(modelName, version);
        if (model != null)
        {
            model.Deactivate();
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ITransformer> LoadModelTransformerAsync(Guid modelId)
    {
        try
        {
            var modelPath = Path.Combine(_modelStoragePath, $"{modelId}.zip");
          
            var mlContext = new MLContext();
            return await Task.Run(() => mlContext.Model.Load(modelPath, out DataViewSchema _));
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Error loading model transformer for {ModelId}", modelId);
            throw new Exception($"Failed to load model transformer: {modelId}", ex);
        }
    }

    public async Task SaveModelTransformerAsync(Guid modelId, ITransformer model)
    {
        try
        {
            var modelPath = Path.Combine(_modelStoragePath, $"{modelId}.zip");
            var mlContext = new MLContext();
           
            await Task.Run(() => mlContext.Model.Save(model, null, modelPath));
            _logger.LogInformation("Successfully saved model transformer {ModelId}", modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving model transformer {ModelId}", modelId);
            throw new Exception($"Failed to save model transformer: {modelId}", ex);
        }
    }
    private async Task SaveModelFileAsync(ITransformer model, string path)
    {
        using var stream = new MemoryStream();
        var mlContext = new MLContext();
        mlContext.Model.Save(model, null, stream);
       // await _fileStorage.SaveFileAsync(path, stream.ToArray());
    }

    private string GetModelPath(string modelName, string version)
    {
        return $"models/{modelName}/{version}/model.zip";
    }
}
