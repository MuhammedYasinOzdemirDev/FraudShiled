namespace FraudShield.TransactionAnalysis.Domain.Models;

public class PCAMetrics
{
    public double[] ExplainedVariance { get; init; }
    public double CumulativeExplainedVariance { get; init; }
    public double ReconstructionError { get; init; }
    public int ComponentCount { get; init; }
}