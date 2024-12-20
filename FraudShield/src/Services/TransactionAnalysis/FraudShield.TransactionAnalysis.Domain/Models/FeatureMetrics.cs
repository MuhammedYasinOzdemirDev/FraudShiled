using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class FeatureMetrics
{
    public int TotalFeatures { get; init; }
    public Dictionary<FeatureType, int> FeaturesByType { get; init; }
    public Dictionary<string, double> NullValueRatios { get; init; }
    public Dictionary<string, Dictionary<string, double>> FeatureCorrelations { get; init; }
    public DateTime CalculatedAt { get; init; }
}