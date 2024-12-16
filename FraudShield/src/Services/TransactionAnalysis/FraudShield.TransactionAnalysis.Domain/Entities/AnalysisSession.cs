using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
using FraudShield.TransactionAnalysis.Domain.Models;

namespace FraudShield.TransactionAnalysis.Domain.Entities;

public class AnalysisSession : AggregateRoot
{
    public Guid AnalysisId { get; private set; }
    public string SessionId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }
    public SessionStatus Status { get; private set; }
    public List<AnalysisStep> Steps { get; private set; }
    public IDictionary<string, object> SessionData { get; private set; }
    public string ModelVersion { get; private set; }

    private AnalysisSession()
    {
        Steps = new List<AnalysisStep>();
        SessionData = new Dictionary<string, object>();
    }

    public static AnalysisSession StartNew(Guid analysisId, string modelVersion)
    {
        var session = new AnalysisSession
        {
            Id = Guid.NewGuid(),
            AnalysisId = analysisId,
            SessionId = $"AS-{Guid.NewGuid():N}",
            StartTime = DateTime.UtcNow,
            Status = SessionStatus.Running,
            ModelVersion = modelVersion
        };

        session.AddDomainEvent(new AnalysisSessionStartedDomainEvent(session));
        return session;
    }

    public void AddStep(string name, string description, StepStatus status)
    {
        Steps.Add(new AnalysisStep(name, description, status));
    }

    public void Complete()
    {
        Status = SessionStatus.Completed;
        EndTime = DateTime.UtcNow;
        AddDomainEvent(new AnalysisSessionCompletedDomainEvent(Id));
    }
}