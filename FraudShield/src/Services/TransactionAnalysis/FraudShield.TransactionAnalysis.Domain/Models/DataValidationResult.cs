namespace FraudShield.TransactionAnalysis.Domain.Models;

public class DataValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; }
    public DataValidationMetrics Metrics { get; init; }
    public DateTime ValidatedAt { get; init; }
}
