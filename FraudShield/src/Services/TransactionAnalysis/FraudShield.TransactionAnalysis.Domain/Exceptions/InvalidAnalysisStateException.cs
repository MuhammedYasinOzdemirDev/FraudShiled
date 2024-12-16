namespace FraudShield.TransactionAnalysis.Domain.Exceptions;

public class InvalidAnalysisStateException : DomainException
{
    public Guid AnalysisId { get; }

    public InvalidAnalysisStateException(Guid analysisId, string message) 
        : base($"Analysis {analysisId}: {message}")
    {
        AnalysisId = analysisId;
    }
}
