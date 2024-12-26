using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class AnalysisResult : Entity
{
    public Guid TransactionId { get; private set; }
    public double AnomalyScore { get; private set; }
    public double FraudProbability { get; private set; }
    public RiskScore RiskScore { get; private set; }
    public List<RiskFactor> RiskFactors { get; private set; }
    public DecisionType Decision { get; private set; }
    public DateTime AnalyzedAt { get; private set; }
    public AnalysisStatus Status { get; private set; }
    public string Error { get; private set; }

    private AnalysisResult()
    {
        RiskFactors = new List<RiskFactor>();
    }

    public static AnalysisResult Create(
        Guid transactionId,
        double anomalyScore,
        double fraudProbability,
        RiskScore riskScore,
        DecisionType decision)
    {
        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            AnomalyScore = anomalyScore,
            FraudProbability = fraudProbability,
            RiskScore = riskScore,
            Decision = decision,
            AnalyzedAt = DateTime.UtcNow,
            Status = AnalysisStatus.Completed
        };
    }

    public static AnalysisResult CreateFailed(Guid transactionId, string errorMessage)
    {
        var result = new AnalysisResult
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            Status = AnalysisStatus.Failed,
            Error = errorMessage,
            AnalyzedAt = DateTime.UtcNow,
            Decision = DecisionType.ReviewRequired
        };

        return result;
    }

    public void AddRiskFactor(RiskFactor factor)
    {
        RiskFactors.Add(factor);
    }

    public void SetError(string error)
    {
        Error = error;
        Status = AnalysisStatus.Failed;
    }
}