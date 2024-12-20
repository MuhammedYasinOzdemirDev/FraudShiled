namespace FraudShield.TransactionAnalysis.Domain.Models;
public class ValidationRuleResult
{
    public bool IsValid { get; init; }
    public List<ValidationError> Errors { get; init; } = new();
}