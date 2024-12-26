using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Events;

public class ModelTrainingStartedEvent : DomainEvent
{
    public Guid ModelId { get; }
    public string ModelName { get; }
    public ModelType Type { get; }
    public DateTime StartedAt { get; }

    public ModelTrainingStartedEvent(Guid modelId, string modelName, ModelType type)
    {
        ModelId = modelId;
        ModelName = modelName;
        Type = type;
        StartedAt = DateTime.UtcNow;
    }
}