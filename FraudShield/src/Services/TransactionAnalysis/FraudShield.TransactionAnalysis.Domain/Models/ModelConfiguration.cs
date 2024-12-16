namespace FraudShield.TransactionAnalysis.Domain.Models;

public record ModelConfiguration
{
    public string ActiveModelVersion { get; init; }
    public double MinimumConfidenceScore { get; init; }
    public int MaximumFeatureCount { get; init; }
    public Dictionary<string, object> HyperParameters { get; init; }
    public bool EnableAutoRetraining { get; init; }
}