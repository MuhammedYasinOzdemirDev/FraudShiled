using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class ValidationError
{
    public string FieldName { get; init; }
    public string ErrorCode { get; init; }
    public string Message { get; init; }
    public ErrorSeverity Severity { get; init; }
    public int RecordIndex { get; init; }
    public string Value { get; init; }
}