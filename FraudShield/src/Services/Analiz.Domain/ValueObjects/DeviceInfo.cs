using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.ValueObjects;

public class DeviceInfo : ValueObject
{
    public string DeviceId { get; private set; }
    public string DeviceType { get; private set; }
    public string IpAddress { get; private set; }
    public string UserAgent { get; private set; }
    
    [NotMapped]
    public Dictionary<string, string> AdditionalInfo { get; private set; }
    
    public string AdditionalInfoJson 
    { 
        get => AdditionalInfo != null ? JsonSerializer.Serialize(AdditionalInfo) : null;
        private set => AdditionalInfo = !string.IsNullOrEmpty(value) 
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(value) 
            : new Dictionary<string, string>();
    }

    private DeviceInfo()
    {
        AdditionalInfo = new Dictionary<string, string>();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DeviceId;
        yield return DeviceType;
        yield return IpAddress;
        yield return UserAgent;
        foreach (var info in AdditionalInfo.OrderBy(x => x.Key))
        {
            yield return info.Key;
            yield return info.Value;
        }
    }
}
