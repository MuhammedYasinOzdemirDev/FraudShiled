using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.ValueObjects;

public class Location : ValueObject
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string Country { get; private set; }
    public string City { get; private set; }

    private Location() { }

    public static Location Create(
        double latitude, 
        double longitude, 
        string country, 
        string city)
    {
        return new Location
        {
            Latitude = latitude,
            Longitude = longitude,
            Country = country,
            City = city
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Country;
        yield return City;
    }
}