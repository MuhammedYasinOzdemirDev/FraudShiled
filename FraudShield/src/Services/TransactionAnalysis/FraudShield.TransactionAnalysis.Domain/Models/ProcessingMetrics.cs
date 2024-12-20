namespace FraudShield.TransactionAnalysis.Domain.Models;

public class ProcessingMetrics
{
    public int TotalRecords { get; init; }
    public int ProcessedRecords { get; init; }
    public int InvalidRecords { get; init; }
    public int OutliersDetected { get; init; }
    public int MissingValuesHandled { get; init; }
    public TimeSpan ProcessingDuration { get; init; }
    public IDictionary<string, int> ErrorCounts { get; init; }
}
