using Analiz.Domain;
using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces;


public interface IRiskScoringService
{
    Task<RiskScore> CalculateRiskScoreAsync(TransactionData data);
    Task<RiskProfile> GetUserRiskProfileAsync(string userId);
    Task<bool> UpdateRiskThresholdsAsync(RiskThresholds thresholds);
    Task<List<RiskFactor>> GetRiskFactorsAsync(Guid transactionId);
    Task<RiskTrends> AnalyzeRiskTrendsAsync(string userId, TimeSpan period);
}