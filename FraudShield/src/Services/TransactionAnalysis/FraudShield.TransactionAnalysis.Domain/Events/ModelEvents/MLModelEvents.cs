using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Entities;

namespace FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;

public class MLModelCreatedDomainEvent : DomainEvent
{
    public MLModel Model { get; }

    public MLModelCreatedDomainEvent(MLModel model)
    {
        Model = model;
    }
}

public class MLModelActivatedDomainEvent : DomainEvent
{
    public Guid ModelId { get; }

    public MLModelActivatedDomainEvent(Guid modelId)
    {
        ModelId = modelId;
    }
}

public class MLModelMetricsUpdatedDomainEvent : DomainEvent
{
    public Guid ModelId { get; }
    public IDictionary<string, double> Metrics { get; }

    public MLModelMetricsUpdatedDomainEvent(Guid modelId, IDictionary<string, double> metrics)
    {
        ModelId = modelId;
        Metrics = metrics;
    }
}

public class MLModelFailedDomainEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string Reason { get; }

    public MLModelFailedDomainEvent(Guid modelId, string reason)
    {
        ModelId = modelId;
        Reason = reason;
    }
}