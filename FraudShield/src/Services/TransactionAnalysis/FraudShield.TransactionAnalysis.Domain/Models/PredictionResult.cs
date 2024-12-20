namespace FraudShield.TransactionAnalysis.Domain.Models;

public class PredictionResult
{
    public float Score { get; init; }
    public float Probability { get; init; }
    public bool PredictedLabel { get; init; }
    public DateTime PredictedAt { get; init; }
    public IDictionary<string, float> Features { get; init; }
}