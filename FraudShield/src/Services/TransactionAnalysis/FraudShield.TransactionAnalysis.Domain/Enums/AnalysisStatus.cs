namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum AnalysisStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    RequiresReview = 4,
    Cancelled = 5
}