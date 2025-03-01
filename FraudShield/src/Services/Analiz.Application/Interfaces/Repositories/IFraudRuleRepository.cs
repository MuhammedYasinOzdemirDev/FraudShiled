using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces.Repositories;

public interface IFraudRuleRepository
{
    Task<List<FraudRule>> GetAllRulesAsync();
    Task<List<FraudRule>> GetActiveRulesAsync();
    Task<FraudRule> GetByIdAsync(Guid id);
    Task<FraudRule> GetByRuleIdAsync(string ruleId);
    Task<FraudRule> AddAsync(FraudRule rule);
    Task UpdateAsync(FraudRule rule);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(string ruleId);
}