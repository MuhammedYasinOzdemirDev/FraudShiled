namespace Analiz.Domain.Entities;

public class RiskEvaluation
{
    public Guid TransactionId { get; set; }
    public RiskScore RiskScore { get; set; }
    public List<RiskFactor> RiskFactors { get; set; }
    public RiskProfile UserRiskProfile { get; set; }
    public DateTime EvaluatedAt { get; set; }
}
