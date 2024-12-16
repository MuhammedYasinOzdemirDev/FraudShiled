namespace FraudShield.TransactionAnalysis.Domain.Models;

public record DeviceInfo
{
    public string DeviceId { get; init; }
    public string DeviceType { get; init; }
    public string OperatingSystem { get; init; }
    public string Browser { get; init; }
    public string UserAgent { get; init; }
    public bool IsMobile { get; init; }
}
