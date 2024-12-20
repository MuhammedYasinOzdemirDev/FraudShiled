namespace FraudShield.TransactionAnalysis.Domain.Models;

public class ValidationMetrics
{
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    public Dictionary<string, int> ErrorCounts { get; set; } = new();
}