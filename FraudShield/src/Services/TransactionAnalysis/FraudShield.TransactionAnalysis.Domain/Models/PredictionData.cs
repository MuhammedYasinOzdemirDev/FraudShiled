namespace FraudShield.TransactionAnalysis.Domain.Models;

public class PredictionData
{
    public float Amount { get; init; }
    public string MerchantId { get; init; }
    public string UserId { get; init; }
    public DateTime TransactionTime { get; init; }
    public string TransactionType { get; init; }
    public IDictionary<string, float> CustomFeatures { get; init; }
}