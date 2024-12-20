namespace FraudShield.TransactionAnalysis.Domain.Models;

public class TransformationResult
{
    public bool IsSuccess { get; init; }
    public string Error { get; init; }
    public int FeaturesAdded { get; init; }
    public Dictionary<string, object> Metrics { get; init; } = new();
}