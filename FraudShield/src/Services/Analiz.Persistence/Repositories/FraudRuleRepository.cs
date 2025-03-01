using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Analiz.Persistence.Repositories;

public class FraudRuleRepository : IFraudRuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public FraudRuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<FraudRule>> GetAllRulesAsync()
    {
        return await _dbContext.FraudRules
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<List<FraudRule>> GetActiveRulesAsync()
    {
        return await _dbContext.FraudRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<FraudRule> GetByIdAsync(Guid id)
    {
        return await _dbContext.FraudRules.FindAsync(id);
    }

    public async Task<FraudRule> GetByRuleIdAsync(string ruleId)
    {
        return await _dbContext.FraudRules
            .FirstOrDefaultAsync(r => r.RuleId == ruleId);
    }

    public async Task<FraudRule> AddAsync(FraudRule rule)
    {
        await _dbContext.FraudRules.AddAsync(rule);
        await _dbContext.SaveChangesAsync();
        return rule;
    }

    public async Task UpdateAsync(FraudRule rule)
    {
        _dbContext.FraudRules.Update(rule);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await GetByIdAsync(id);
        if (rule != null)
        {
            _dbContext.FraudRules.Remove(rule);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string ruleId)
    {
        return await _dbContext.FraudRules
            .AnyAsync(r => r.RuleId == ruleId);
    }
}