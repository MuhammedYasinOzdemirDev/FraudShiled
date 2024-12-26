using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class FraudAlert : Entity
{
    public Guid TransactionId { get; private set; }
    public string UserId { get; private set; }
    public AlertType Type { get; private set; }
    public AlertStatus Status { get; private set; }
    public RiskScore RiskScore { get; private set; }
    public List<string> Factors { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string Resolution { get; private set; }

    private FraudAlert()
    {
        Factors = new List<string>();
    }

    public static FraudAlert Create(
        Guid transactionId,
        string userId,
        RiskScore riskScore,
        List<string> factors)
    {
        return new FraudAlert
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = userId,
            Type = DetermineAlertType(riskScore.Level),
            Status = AlertStatus.Active,
            RiskScore = riskScore,
            Factors = factors,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Resolve(string resolution)
    {
        Status = AlertStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
    }

    private static AlertType DetermineAlertType(RiskLevel riskLevel)
    {
        return riskLevel switch
        {
            RiskLevel.Critical => AlertType.Critical,
            RiskLevel.High => AlertType.High,
            _ => AlertType.Medium
        };
    }
}