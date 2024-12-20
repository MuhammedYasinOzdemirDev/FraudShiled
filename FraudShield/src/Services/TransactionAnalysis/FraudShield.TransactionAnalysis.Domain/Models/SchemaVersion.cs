namespace FraudShield.TransactionAnalysis.Domain.Models;

public class SchemaVersion
{
    public int Major { get; init; }
    public int Minor { get; init; }
    public string Description { get; init; }
    public DateTime ReleasedAt { get; init; }
}