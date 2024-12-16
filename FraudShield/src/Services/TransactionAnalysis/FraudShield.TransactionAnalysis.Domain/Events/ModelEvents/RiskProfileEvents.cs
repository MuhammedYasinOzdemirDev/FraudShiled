using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;

public class RiskProfileCreatedDomainEvent : DomainEvent
{
    public RiskProfile Profile { get; }

    public RiskProfileCreatedDomainEvent(RiskProfile profile)
    {
        Profile = profile;
    }
}

public class RiskProfileUpdatedDomainEvent : DomainEvent
{
    public Guid ProfileId { get; }
    public decimal NewScore { get; }
    public RiskLevel NewLevel { get; }

    public RiskProfileUpdatedDomainEvent(Guid profileId, decimal newScore, RiskLevel newLevel)
    {
        ProfileId = profileId;
        NewScore = newScore;
        NewLevel = newLevel;
    }
}