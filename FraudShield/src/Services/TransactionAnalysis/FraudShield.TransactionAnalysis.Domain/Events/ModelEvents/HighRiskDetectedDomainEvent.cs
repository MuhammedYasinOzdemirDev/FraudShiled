using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.ValueObjects;

namespace FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
public class HighRiskDetectedDomainEvent : DomainEvent
{
    public Guid AnalysisId { get; }
    public Guid TransactionId { get; }
    public RiskScore RiskScore { get; }
    public IReadOnlyList<RiskFactor> RiskFactors { get; }

    public HighRiskDetectedDomainEvent(
        Guid analysisId,
        Guid transactionId,
        RiskScore riskScore,
        IReadOnlyList<RiskFactor> riskFactors)
    {
        AnalysisId = analysisId;
        TransactionId = transactionId;
        RiskScore = riskScore;
        RiskFactors = riskFactors;
    }
}