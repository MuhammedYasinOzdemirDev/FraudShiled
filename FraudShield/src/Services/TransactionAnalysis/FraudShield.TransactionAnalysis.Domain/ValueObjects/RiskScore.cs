using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudShield.TransactionAnalysis.Domain.ValueObjects;
[Owned]
public class RiskScore : ValueObject
{
    public decimal Score { get; private set; }
    public RiskLevel Level { get; private set; }
    public List<string> Indicators { get; private set; }
    public DateTime CalculatedAt { get; private set; }  // Eklendi
    public IDictionary<string, decimal> FactorWeights { get; private set; }  // Eklendi
    
    private RiskScore(decimal score, RiskLevel level, List<string> indicators)
    {
        Score = score;
        Level = level;
        Indicators = indicators;
        CalculatedAt = DateTime.UtcNow;
        FactorWeights = new Dictionary<string, decimal>();
    }
    
    public static RiskScore Calculate(
        decimal score, 
        List<string> indicators,
        IDictionary<string, decimal> factorWeights = null)
    {
        var level = score switch
        {
            var s when s >= 0.8m => RiskLevel.Critical,
            var s when s >= 0.6m => RiskLevel.High,
            var s when s >= 0.4m => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
        
        var riskScore = new RiskScore(score, level, indicators);
        if (factorWeights != null)
        {
            riskScore.FactorWeights = factorWeights;
        }
        
        return riskScore;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Score;
        yield return Level;
        yield return CalculatedAt;
        foreach (var indicator in Indicators)
            yield return indicator;
        foreach (var weight in FactorWeights)
            yield return $"{weight.Key}:{weight.Value}";
    }
}