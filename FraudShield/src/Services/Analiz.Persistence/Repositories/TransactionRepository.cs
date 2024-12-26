using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(
        ApplicationDbContext context,
        ILogger<TransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Transaction> GetTransactionAsync(Guid transactionId)
    {
        return await _context.Transactions
            .Include(t => t.Details)
            .Include(t => t.DeviceInfo)
            .Include(t => t.Location)
            .Include(t => t.RiskScore)
            .FirstOrDefaultAsync(t => t.Id == transactionId);
    }

    public async Task<List<Transaction>> GetUserTransactionsAsync(string userId, TimeSpan period)
    {
        var startDate = DateTime.UtcNow.Subtract(period);
        
        return await _context.Transactions
            .Include(t => t.RiskScore)
            .Where(t => t.UserId == userId && t.TransactionTime >= startDate)
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsBetweenDatesAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        return await _context.Transactions
            .Include(t => t.RiskScore)
            .Where(t => t.TransactionTime >= startDate && t.TransactionTime <= endDate)
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    public async Task<Transaction> SaveTransactionAsync(Transaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

  
    public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
    {
        try
        {
            var result = await _context.AnalysisResults
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
            return await _context.FraudAlerts
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
            var existingRule = await _context.FraudRules
                .FirstOrDefaultAsync(r => r.RuleId == rule.RuleId && !r.IsDeleted);

            if (existingRule == null)
            {
                _context.FraudRules.Add(rule);
            }
            else
            {
                existingRule.Update(rule);
                _context.FraudRules.Update(existingRule);
            }

            await _context.SaveChangesAsync();
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
        var transaction = await _context.Transactions.FindAsync(transactionId);
        if (transaction != null)
        {
            transaction.UpdateStatus(status);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid transactionId)
    {
        return await _context.Transactions.AnyAsync(t => t.Id == transactionId);
    }
}
