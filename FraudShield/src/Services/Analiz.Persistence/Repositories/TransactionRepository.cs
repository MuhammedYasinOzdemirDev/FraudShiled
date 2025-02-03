using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<TransactionRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<Transaction> GetTransactionAsync(Guid transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions
            .Include(t => t.Details)
            .Include(t => t.DeviceInfo)
            .Include(t => t.Location)
            .Include(t => t.RiskScore)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<List<Transaction>> GetUserTransactionsAsync(string userId, TimeSpan period)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var startDate = DateTime.UtcNow.Subtract(period);
    
        return await context.Transactions
            .Include(t => t.RiskScore)
            .Where(t => t.UserId == userId && 
                        t.TransactionTime >= startDate &&
                        !t.IsDeleted)  
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsBetweenDatesAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions
            .Include(t => t.RiskScore)
            .Where(t => t.TransactionTime >= startDate && t.TransactionTime <= endDate)
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    public async Task<Transaction> SaveTransactionAsync(Transaction transaction)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.Transactions.AddAsync(transaction);
        await context.SaveChangesAsync();
        return transaction;
    }

    public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var result = await context.AnalysisResults
                .Include(a => a.RiskFactors)
                .FirstOrDefaultAsync(a => a.Id == analysisId && !a.IsDeleted);

            if (result == null)
            {
                _logger.LogWarning("Analysis result not found for ID: {AnalysisId}", analysisId);
                throw new Exception($"Analysis result not found with ID: {analysisId}");
            }

            return result;
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Error retrieving analysis result {AnalysisId}", analysisId);
            throw new RepositoryException($"Error retrieving analysis result: {analysisId}", ex);
        }
    }

    public async Task<List<FraudAlert>> GetActiveAlertsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FraudAlerts
                .Include(a => a.RiskScore)
                .Include(a => a.Factors)
                .Where(a => a.Status == AlertStatus.Active && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active fraud alerts");
            throw new RepositoryException("Error retrieving active fraud alerts", ex);
        }
    }

    public async Task UpdateFraudRuleAsync(FraudRule rule)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existingRule = await context.FraudRules
                .FirstOrDefaultAsync(r => r.RuleId == rule.RuleId && !r.IsDeleted);

            if (existingRule == null)
            {
                context.FraudRules.Add(rule);
            }
            else
            {
                existingRule.Update(rule);
                context.FraudRules.Update(existingRule);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated fraud rule: {RuleId}", rule.RuleId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating fraud rule {RuleId}", rule.RuleId);
            throw new ConcurrencyException($"Concurrency error updating fraud rule: {rule.RuleId}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rule {RuleId}", rule.RuleId);
            throw new RepositoryException($"Error updating fraud rule: {rule.RuleId}", ex);
        }
    }
 
    public async Task UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var transaction = await context.Transactions.FindAsync(transactionId);
        if (transaction != null)
        {
            transaction.UpdateStatus(status);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid transactionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Transactions.AnyAsync(t => t.Id == transactionId);
    }
}