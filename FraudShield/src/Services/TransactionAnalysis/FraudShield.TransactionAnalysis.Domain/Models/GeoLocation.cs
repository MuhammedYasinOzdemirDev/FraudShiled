namespace FraudShield.TransactionAnalysis.Domain.Models;

public record GeoLocation
{
    public string CountryCode { get; init; }
    public string City { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string IpAddress { get; init; }
}
