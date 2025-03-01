using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Interfaces.Repositories;

public interface IFraudAlertRepository
{
    Task<List<FraudAlert>> GetAllAsync();
    Task<List<FraudAlert>> GetActiveAlertsAsync();
    Task<List<FraudAlert>> GetByUserIdAsync(string userId);
    Task<List<FraudAlert>> GetByTransactionIdAsync(Guid transactionId);
    Task<List<FraudAlert>> GetByStatusAsync(AlertStatus status);
    Task<List<FraudAlert>> GetBySeverityAsync(AlertSeverity severity);
    Task<FraudAlert> GetByIdAsync(Guid id);
    Task<FraudAlert> AddAsync(FraudAlert alert);
    Task UpdateAsync(FraudAlert alert);
    Task DeleteAsync(Guid id);
}