using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
using FraudShield.TransactionAnalysis.Domain.Models;
using FraudShield.TransactionAnalysis.Domain.Rules;

namespace FraudShield.TransactionAnalysis.Domain.Entities;

public class RiskProfile : AggregateRoot
{
    public string UserId { get; private set; }
    public decimal BaseRiskScore { get; private set; }
    public List<HistoricalRiskFactor> HistoricalFactors { get; private set; }
    public List<VelocityRule> VelocityRules { get; private set; }
    public DateTime LastUpdated { get; private set; }
    public IDictionary<string, decimal> RiskMetrics { get; private set; }
    public RiskLevel CurrentRiskLevel { get; private set; }
    public bool IsActive { get; private set; }
    public IDictionary<string, string> UserAttributes { get; private set; }
    private readonly List<HistoricalRiskFactor> _historicalFactors;
    private RiskProfile()
    {
        HistoricalFactors = new List<HistoricalRiskFactor>();
        VelocityRules = new List<VelocityRule>();
        RiskMetrics = new Dictionary<string, decimal>();
        UserAttributes = new Dictionary<string, string>();
    }

    public static RiskProfile CreateNew(
        string userId,
        IDictionary<string, string> attributes = null)
    {
        var profile = new RiskProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            BaseRiskScore = 0.0m,
            CurrentRiskLevel = RiskLevel.Low,
            LastUpdated = DateTime.UtcNow,
            IsActive = true,
            UserAttributes = attributes ?? new Dictionary<string, string>()
        };

        profile.AddDomainEvent(new RiskProfileCreatedDomainEvent(profile));
        return profile;
    }
    private void AddHistoricalFactor(string reason, decimal score)
    {
        var factor = new HistoricalRiskFactor(reason, score, DateTime.UtcNow);
        _historicalFactors.Add(factor);
    }

    public void UpdateRiskScore(decimal newScore, string reason)
    {
        BaseRiskScore = newScore;
        CurrentRiskLevel = DetermineRiskLevel(newScore);
        LastUpdated = DateTime.UtcNow;
        AddHistoricalFactor(reason, newScore);
        
        AddDomainEvent(new RiskProfileUpdatedDomainEvent(Id, newScore, CurrentRiskLevel));
    }
    

    private RiskLevel DetermineRiskLevel(decimal score) =>
        score switch
        {
            var s when s >= 0.8m => RiskLevel.Critical,
            var s when s >= 0.6m => RiskLevel.High,
            var s when s >= 0.4m => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
}