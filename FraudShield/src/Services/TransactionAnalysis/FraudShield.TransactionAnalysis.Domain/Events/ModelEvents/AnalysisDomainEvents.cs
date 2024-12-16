using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.ValueObjects;

namespace FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;

public class AnalysisInitiatedDomainEvent : DomainEvent
{
    public Analysis Analysis { get; }

    public AnalysisInitiatedDomainEvent(Analysis analysis)
    {
        Analysis = analysis;
    }
}

public class RiskScoreUpdatedDomainEvent : DomainEvent
{
    public Guid AnalysisId { get; }
    public Guid TransactionId { get; }
    public RiskScore RiskScore { get; }

    public RiskScoreUpdatedDomainEvent(Guid analysisId, Guid transactionId, RiskScore riskScore)
    {
        AnalysisId = analysisId;
        TransactionId = transactionId;
        RiskScore = riskScore;
    }
}

public class RiskFactorAddedDomainEvent : DomainEvent
{
    public Guid AnalysisId { get; }
    public RiskFactor Factor { get; }

    public RiskFactorAddedDomainEvent(Guid analysisId, RiskFactor factor)
    {
        AnalysisId = analysisId;
        Factor = factor;
    }
}

public class AnalysisMetricsUpdatedDomainEvent : DomainEvent
{
    public Guid AnalysisId { get; }
    public AnalysisMetrics Metrics { get; }

    public AnalysisMetricsUpdatedDomainEvent(Guid analysisId, AnalysisMetrics metrics)
    {
        AnalysisId = analysisId;
        Metrics = metrics;
    }
}

public class AnalysisCompletedDomainEvent : DomainEvent
{
    public Analysis Analysis { get; }

    public AnalysisCompletedDomainEvent(Analysis analysis)
    {
        Analysis = analysis;
    }
}

public class AnalysisFailedDomainEvent : DomainEvent
{
    public Guid AnalysisId { get; }
    public string Reason { get; }

    public AnalysisFailedDomainEvent(Guid analysisId, string reason)
    {
        AnalysisId = analysisId;
        Reason = reason;
    }
}