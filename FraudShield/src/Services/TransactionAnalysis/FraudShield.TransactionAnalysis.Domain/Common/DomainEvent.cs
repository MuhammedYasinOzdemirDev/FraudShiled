using MediatR;

namespace FraudShield.TransactionAnalysis.Domain.Common;

public abstract class DomainEvent : INotification
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }

    protected DomainEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }
}