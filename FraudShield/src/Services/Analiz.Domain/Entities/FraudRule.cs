using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class FraudRule : Entity
{
    public string RuleId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Condition { get; private set; }
    public RuleAction Action { get; private set; }
    public decimal Threshold { get; private set; }
    public bool IsActive { get; private set; }
    public RulePriority Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastModifiedAt { get; private set; }

    private FraudRule() { }

    public static FraudRule Create(
        string name,
        string description,
        string condition,
        RuleAction action,
        decimal threshold,
        RulePriority priority)
    {
        return new FraudRule
        {
            Id = Guid.NewGuid(),
            RuleId = GenerateRuleId(name),
            Name = name,
            Description = description,
            Condition = condition,
            Action = action,
            Threshold = threshold,
            IsActive = true,
            Priority = priority,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(FraudRule updatedRule)
    {
        Name = updatedRule.Name;
        Description = updatedRule.Description;
        Condition = updatedRule.Condition;
        Action = updatedRule.Action;
        Threshold = updatedRule.Threshold;
        Priority = updatedRule.Priority;
        LastModifiedAt = DateTime.UtcNow;
    }

    private static string GenerateRuleId(string name)
    {
        return $"RULE_{name.ToUpper().Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd}";
    }
}
