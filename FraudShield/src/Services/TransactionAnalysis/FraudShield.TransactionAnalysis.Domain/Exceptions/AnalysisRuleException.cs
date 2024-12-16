namespace FraudShield.TransactionAnalysis.Domain.Exceptions;

public class AnalysisRuleException : DomainException
{
    public string RuleName { get; }
    public string Details { get; }

    public AnalysisRuleException(string ruleName, string message)
        : base($"Analysis Rule {ruleName}: {message}")
    {
        RuleName = ruleName;
        Details = message;
    }
}