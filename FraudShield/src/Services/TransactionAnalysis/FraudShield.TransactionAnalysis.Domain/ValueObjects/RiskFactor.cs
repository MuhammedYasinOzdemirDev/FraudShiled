using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.ValueObjects;

public class RiskFactor : ValueObject
{
    public string Code { get; private set; }
    public string Description { get; private set; }
    public decimal Impact { get; private set; }
    public RiskFactorCategory Category { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public IDictionary<string, string> Evidence { get; private set; }

    private RiskFactor()
    {
        Evidence = new Dictionary<string, string>();
    }

    public static RiskFactor Create(
        string code,
        string description,
        decimal impact,
        RiskFactorCategory category,
        IDictionary<string, string> evidence = null)
    {
        return new RiskFactor
        {
            Code = code,
            Description = description,
            Impact = impact,
            Category = category,
            DetectedAt = DateTime.UtcNow,
            Evidence = evidence ?? new Dictionary<string, string>()
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return Impact;
        yield return Category;
        yield return DetectedAt;
        foreach (var item in Evidence)
        {
            yield return $"{item.Key}:{item.Value}";
        }
    }
}
