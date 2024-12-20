namespace FraudShield.TransactionAnalysis.Domain.Models;

public class TransactionRecord
{
    public string TransactionId { get; init; }
    public string UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public DateTime Timestamp { get; init; }
    public string MerchantId { get; init; }
    public string TransactionType { get; init; }
    public GeoLocation Location { get; init; }
    public IDictionary<string, object> AdditionalFields { get; init; }
}