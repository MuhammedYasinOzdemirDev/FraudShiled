using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces;

public interface IFraudDetectionService
{
    Task<AnalysisResult> AnalyzeTransactionAsync(TransactionRequest request);
    Task<List<AnalysisResult>> AnalyzeBatchAsync(List<TransactionRequest> requests);
    Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId);
    Task<RiskEvaluation> EvaluateRiskAsync(TransactionData data);
    Task<bool> UpdateFraudRulesAsync(List<FraudRule> rules);
    Task<List<FraudAlert>> GetActiveAlertsAsync();
}
