namespace FraudShield.TransactionAnalysis.Domain.Models;

public class DataValidationMetrics
{
    public int TotalRecords { get; init; }
    public int ValidRecords { get; init; }
    public int InvalidRecords { get; init; }
    public IDictionary<string, int> ErrorCounts { get; init; }
    public TimeSpan ValidationDuration { get; init; }
}