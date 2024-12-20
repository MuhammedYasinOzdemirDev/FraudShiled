using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class FieldDefinition
{
    public string Name { get; init; }
    public FieldType Type { get; init; }
    public bool IsRequired { get; init; }
    public string Format { get; init; }
    public string Description { get; init; }
}