namespace Analiz.Domain.Entities;

public class RuleResult
{
    public string RuleId { get; set; }
    public string RuleName { get; set; }
    public bool IsTriggered { get; set; }
    public RuleAction Action { get; set; }
    public double Confidence { get; set; }
}
