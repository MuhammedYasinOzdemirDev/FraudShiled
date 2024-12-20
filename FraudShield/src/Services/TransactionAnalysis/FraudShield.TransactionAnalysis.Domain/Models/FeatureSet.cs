using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class FeatureSet
{
    public IDataView OriginalData { get; set; }
    public List<Feature> Features { get; } = new();
    public FeatureMetrics Metrics { get; set; }
    public DateTime CreatedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }

    public void AddFeature(Feature feature)
    {
        Features.Add(feature);
    }
}