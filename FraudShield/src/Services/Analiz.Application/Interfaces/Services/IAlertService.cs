using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces;

public interface IAlertService
{
    Task<FraudAlert> CreateAlertAsync(TransactionData transaction, AnalysisResult analysisResult, AlertSeverity severity);
    Task<List<FraudAlert>> GetActiveAlertsAsync();
    Task<List<FraudAlert>> GetAlertsByUserIdAsync(string userId);
    Task<FraudAlert> GetAlertByIdAsync(Guid alertId);
    Task<FraudAlert> ResolveAlertAsync(Guid alertId, string resolution, string resolvedBy);
    Task<FraudAlert> AssignAlertAsync(Guid alertId, string assignedTo);
    Task<List<FraudAlert>> GetAlertsByStatusAsync(AlertStatus status);
    Task<List<FraudAlert>> GetAlertsBySeverityAsync(AlertSeverity severity);
    Task<AlertSummary> GetAlertSummaryAsync();
}