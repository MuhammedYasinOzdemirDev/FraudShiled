using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Entities;

namespace FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;

public class AnalysisRuleCreatedDomainEvent : DomainEvent
{
    public AnalysisRule Rule { get; }

    public AnalysisRuleCreatedDomainEvent(AnalysisRule rule)
    {
        Rule = rule;
    }
}

public class RuleTriggeredDomainEvent : DomainEvent
{
    public Guid RuleId { get; }
    public string RuleName { get; }

    public RuleTriggeredDomainEvent(Guid ruleId, string ruleName)
    {
        RuleId = ruleId;
        RuleName = ruleName;
    }
}
