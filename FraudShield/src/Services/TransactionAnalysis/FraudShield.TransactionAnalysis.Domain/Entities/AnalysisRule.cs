using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
using FraudShield.TransactionAnalysis.Domain.Models;

namespace FraudShield.TransactionAnalysis.Domain.Entities;

public class AnalysisRule : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RuleCondition Condition { get; private set; }
    public decimal RiskImpact { get; private set; }
    public bool IsActive { get; private set; }
    public RuleCategory Category { get; private set; }
    public int Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastTriggered { get; private set; }
    public int TriggerCount { get; private set; }
    // Configuration tipini string olarak değiştirdik
    public IDictionary<string, string> Configuration { get; private set; }

    private AnalysisRule()
    {
        Configuration = new Dictionary<string, string>();
    }

    public static AnalysisRule Create(
        string name,
        string description,
        RuleCondition condition,
        decimal riskImpact,
        RuleCategory category,
        int priority,
        IDictionary<string, string> configuration = null)
    {
        var rule = new AnalysisRule
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Condition = condition,
            RiskImpact = riskImpact,
            Category = category,
            Priority = priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Configuration = configuration ?? new Dictionary<string, string>()
        };

        rule.AddDomainEvent(new AnalysisRuleCreatedDomainEvent(rule));
        return rule;
    }

    public void UpdateConfiguration(IDictionary<string, string> configuration)
    {
        Configuration = configuration ?? new Dictionary<string, string>();
    }

    public void Trigger()
    {
        TriggerCount++;
        LastTriggered = DateTime.UtcNow;
        AddDomainEvent(new RuleTriggeredDomainEvent(Id, Name));
    }
}