namespace FraudShield.TransactionAnalysis.Domain.Models;

public record TransactionData
{
    public Guid TransactionId { get; init; }
    public string UserId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; }
    public DateTime Timestamp { get; init; }
    public IDictionary<string, string> Metadata { get; init; }
    public GeoLocation Location { get; init; }
    public DeviceInfo Device { get; init; }
}