namespace Analiz.Domain.Entities;

public class RuleAction
{
    public string ActionType { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public decimal ThresholdValue { get; set; }
    public bool IsBlocking { get; set; }
}