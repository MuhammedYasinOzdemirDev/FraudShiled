using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;


public class AnalysisResultRepository : IAnalysisResultRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnalysisResultRepository> _logger;

    public AnalysisResultRepository(
        ApplicationDbContext context,
        ILogger<AnalysisResultRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
    {
        return await _context.AnalysisResults
            .Include(a => a.RiskFactors)
            .FirstOrDefaultAsync(a => a.Id == analysisId);
    }

    public async Task<List<AnalysisResult>> GetResultsByDateRangeAsync(
        DateTime startDate, 
        DateTime endDate)
    {
        return await _context.AnalysisResults
            .Include(r => r.RiskFactors)
            .Where(r => r.AnalyzedAt >= startDate && r.AnalyzedAt <= endDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();
    }

    public async Task<List<AnalysisResult>> GetHighRiskResultsAsync(TimeSpan period)
    {
        var startDate = DateTime.UtcNow.Subtract(period);
        
        return await _context.AnalysisResults
            .Include(r => r.RiskFactors)
            .Where(r => r.RiskScore.Level >= RiskLevel.High && r.AnalyzedAt >= startDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();
    }

    public async Task<AnalysisResult> SaveResultAsync(AnalysisResult result)
    {
        await _context.AnalysisResults.AddAsync(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task DeleteOldResultsAsync(DateTime cutoffDate)
    {
        var oldResults = await _context.AnalysisResults
            .Where(r => r.AnalyzedAt < cutoffDate)
            .ToListAsync();

        _context.AnalysisResults.RemoveRange(oldResults);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AnalysisResult>> GetUserResultsAsync(string userId, TimeSpan period)
    {
        var startDate = DateTime.UtcNow.Subtract(period);
        
        return await _context.AnalysisResults
            .Include(r => r.RiskFactors)
            .Where(r => r.AnalyzedAt >= startDate)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync();
    }
}