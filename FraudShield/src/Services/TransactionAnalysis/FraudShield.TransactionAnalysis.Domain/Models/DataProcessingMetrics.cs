namespace FraudShield.TransactionAnalysis.Domain.Models;

public class DataProcessingMetrics
{
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SkippedRecords { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, int> MissingValueCounts { get; set; }
    public Dictionary<string, int> OutlierCounts { get; set; }
}