namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum RiskRuleCategory
{
    TransactionAmount = 0,
    LocationBased = 1,
    TimeBased = 2,
    DeviceBased = 3,
    UserBehavior = 4,
    VelocityCheck = 5
}
