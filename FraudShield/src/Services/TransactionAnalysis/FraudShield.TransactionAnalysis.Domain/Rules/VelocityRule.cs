using FraudShield.TransactionAnalysis.Domain.Common;

namespace FraudShield.TransactionAnalysis.Domain.Rules;

public class VelocityRule : ValueObject
{
    public string Name { get; private set; }
    public TimeWindow TimeWindow { get; private set; }
    public int MaximumAllowed { get; private set; }
    public string RuleType { get; private set; }
    public decimal RiskScore { get; private set; }
    public bool IsActive { get; private set; }

    private VelocityRule() { }

    public static VelocityRule Create(
        string name,
        TimeWindow timeWindow,
        int maximumAllowed,
        string ruleType,
        decimal riskScore)
    {
        return new VelocityRule
        {
            Name = name,
            TimeWindow = timeWindow,
            MaximumAllowed = maximumAllowed,
            RuleType = ruleType,
            RiskScore = riskScore,
            IsActive = true
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return TimeWindow;
        yield return MaximumAllowed;
        yield return RuleType;
        yield return RiskScore;
    }
}
public record TimeWindow
{
    public int Value { get; init; }
    public TimeWindowUnit Unit { get; init; }

    public static TimeWindow Create(int value, TimeWindowUnit unit)
        => new() { Value = value, Unit = unit };
}

public enum TimeWindowUnit
{
    Minutes,
    Hours,
    Days
}