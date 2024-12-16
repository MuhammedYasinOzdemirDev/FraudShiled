namespace FraudShield.TransactionAnalysis.Domain.Models;

public record AnalysisSettings
{
    public bool EnableRealTimeAnalysis { get; init; }
    public int BatchSize { get; init; }
    public TimeSpan AnalysisTimeout { get; init; }
    public Dictionary<string, double> Thresholds { get; init; }
    public List<string> EnabledRules { get; init; }
    public ModelConfiguration ModelConfig { get; init; }
}
