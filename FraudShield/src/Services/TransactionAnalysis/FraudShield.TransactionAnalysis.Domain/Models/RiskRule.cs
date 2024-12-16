using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class RiskRule
{
    public string RuleId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RuleCondition Condition { get; private set; }
    public decimal RiskImpact { get; private set; }
    public bool IsActive { get; private set; }
    public RiskRuleCategory Category { get; private set; }

    private RiskRule() { }

    public static RiskRule Create(
        string name, 
        string description, 
        RuleCondition condition,
        decimal riskImpact,
        RiskRuleCategory category)
    {
        return new RiskRule
        {
            RuleId = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Condition = condition,
            RiskImpact = riskImpact,
            IsActive = true,
            Category = category
        };
    }
}