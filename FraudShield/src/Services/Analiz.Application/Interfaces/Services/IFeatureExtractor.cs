using Analiz.Application.Feature;
using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces;


public interface IFeatureExtractionService
{
    Task<FeatureSet> ExtractFeaturesAsync(TransactionData data);
    Task<List<FeatureSet>> ExtractBatchFeaturesAsync(List<TransactionData> data);
    Task<FeatureImportance> GetFeatureImportanceAsync(string modelName);
    Task<bool> UpdateFeatureConfigurationAsync(FeatureConfig config);
}