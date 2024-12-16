using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Entities;

namespace FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
public class AnalysisSessionStartedDomainEvent : DomainEvent
{
    public AnalysisSession Session { get; }

    public AnalysisSessionStartedDomainEvent(AnalysisSession session)
    {
        Session = session;
    }
}

public class AnalysisSessionCompletedDomainEvent : DomainEvent
{
    public Guid SessionId { get; }

    public AnalysisSessionCompletedDomainEvent(Guid sessionId)
    {
        SessionId = sessionId;
    }
}