namespace FraudShield.TransactionAnalysis.Domain.Models;

public class DataSchema
{
    public List<FieldDefinition> Fields { get; init; }
    public string Version { get; init; }
    public DateTime CreatedAt { get; init; }

    public DataSchema()
    {
        Fields = new List<FieldDefinition>();
        CreatedAt = DateTime.UtcNow;
        Version = "1.0";
    }
}
