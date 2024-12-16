using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Events.ModelEvents;
using FraudShield.TransactionAnalysis.Domain.Exceptions;
using FraudShield.TransactionAnalysis.Domain.ValueObjects;

namespace FraudShield.TransactionAnalysis.Domain.Entities;

public class Analysis : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public string UserId { get; private set; }  // Eklendi
    public RiskScore RiskScore { get; private set; }
    public List<RiskFactor> RiskFactors { get; private set; }
    public AnalysisMetrics Metrics { get; private set; }
    public DateTime AnalyzedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }  // Eklendi
    public string ModelVersion { get; private set; }
    public AnalysisStatus Status { get; private set; }  // Eklendi
    public IDictionary<string, string> Metadata { get; private set; }  // Eklendi
    
    private Analysis() 
    {
        RiskFactors = new List<RiskFactor>();
        Metadata = new Dictionary<string, string>();
    }
    
    public static Analysis Create(
        Guid transactionId, 
        string userId,  // Yeni parametre
        string modelVersion)
    {
        var analysis = new Analysis
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = userId,
            AnalyzedAt = DateTime.UtcNow,
            ModelVersion = modelVersion,
            Status = AnalysisStatus.Pending,
            RiskFactors = new List<RiskFactor>(),
            Metrics = AnalysisMetrics.Initial()
        };
        
        analysis.AddDomainEvent(new AnalysisInitiatedDomainEvent(analysis));
        return analysis;
    }
    
    public void SetRiskScore(RiskScore score)
    {
        RiskScore = score;
        
        // Status güncellemesi eklendi
        Status = score.Level switch
        {
            RiskLevel.Critical => AnalysisStatus.RequiresReview,
            RiskLevel.High => AnalysisStatus.RequiresReview,
            _ => Status
        };
        
        AddDomainEvent(new RiskScoreUpdatedDomainEvent(Id, TransactionId, score));
    }
    
    public void AddRiskFactor(RiskFactor factor)
    {
        RiskFactors.Add(factor);
        AddDomainEvent(new RiskFactorAddedDomainEvent(Id, factor));  // Yeni event
    }
    
    public void UpdateMetrics(AnalysisMetrics metrics)
    {
        Metrics = metrics;
        AddDomainEvent(new AnalysisMetricsUpdatedDomainEvent(Id, metrics));  // Yeni event
    }

    // Yeni metodlar
    public void Complete()
    {
        if (Status == AnalysisStatus.Failed)
            throw new InvalidAnalysisStateException(Id, "Failed analyses cannot be completed");

        Status = AnalysisStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AddDomainEvent(new AnalysisCompletedDomainEvent(this));
    }

    public void Fail(string reason)
    {
        Status = AnalysisStatus.Failed;
        Metadata["FailureReason"] = reason;
        AddDomainEvent(new AnalysisFailedDomainEvent(Id, reason));
    }

    public void AddMetadata(string key, string value)
    {
        Metadata[key] = value;
    }
}
