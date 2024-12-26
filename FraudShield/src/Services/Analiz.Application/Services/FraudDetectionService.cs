using Analiz.Application.Feature;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Events;
using Analiz.ML.Utils;
using FraudShield.TransactionAnalysis.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

public class FraudDetectionService : IFraudDetectionService
{
    private readonly IModelService _modelService;
    private readonly IRiskScoringService _riskScoring;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILogger<FraudDetectionService> _logger;
    private readonly IPublisher _publisher;

    public FraudDetectionService(
        IModelService modelService,
        IRiskScoringService riskScoring,
        IFeatureExtractionService featureExtractor,
        ITransactionRepository transactionRepository,
        ILogger<FraudDetectionService> logger,
        IPublisher publisher)
    {
        _modelService = modelService;
        _riskScoring = riskScoring;
        _featureExtractor = featureExtractor;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _publisher = publisher;
    }

   

    public async Task<List<AnalysisResult>> AnalyzeBatchAsync(List<TransactionRequest> requests)
    {
        var results = new List<AnalysisResult>();
        foreach (var request in requests)
        {
            try
            {
                var result = await AnalyzeTransactionAsync(request);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing transaction in batch {TransactionId}", 
                    request.TransactionId);
            
                results.Add(AnalysisResult.CreateFailed(request.TransactionId, ex.Message));
            }
        }
        return results;
    }


    public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
    {
        return await _transactionRepository.GetAnalysisResultAsync(analysisId);
    }

    public async Task<RiskEvaluation> EvaluateRiskAsync(TransactionData data)
    {
        var features = await _featureExtractor.ExtractFeaturesAsync(data);
        var riskScore = await _riskScoring.CalculateRiskScoreAsync(data);
        var riskFactors = await _riskScoring.GetRiskFactorsAsync(data.TransactionId);

        return new RiskEvaluation
        {
            TransactionId = data.TransactionId,
            RiskScore = riskScore,
            RiskFactors = riskFactors,
            UserRiskProfile = await _riskScoring.GetUserRiskProfileAsync(data.UserId),
            EvaluatedAt = DateTime.UtcNow
        };
    }

    public async Task<bool> UpdateFraudRulesAsync(List<FraudRule> rules)
    {
        try
        {
            // Kural validasyonu
            if (!ValidateFraudRules(rules))
                return false;

            // Kuralları güncelle
            foreach (var rule in rules)
            {
                await UpdateRule(rule);
            }

            // Event yayınla
            await _publisher.Publish(new FraudRulesUpdatedEvent(rules));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraud rules");
            return false;
        }
    }

    public async Task<List<FraudAlert>> GetActiveAlertsAsync()
    {
        return await _transactionRepository.GetActiveAlertsAsync();
    }
    
    
    public async Task<AnalysisResult> AnalyzeTransactionAsync(TransactionRequest request)
    {
        try
        {
            _logger.LogInformation("Starting fraud analysis for transaction {TransactionId}", 
                request.TransactionId);

            var features = await _featureExtractor.ExtractFeaturesAsync(request.TransactionData);
            var anomalyScore = await DetectAnomaliesAsync(features);
            var fraudProbability = await ClassifyTransactionAsync(features);
            var riskScore = await _riskScoring.CalculateRiskScoreAsync(request.TransactionData);
            var riskFactors = await _riskScoring.GetRiskFactorsAsync(request.TransactionId);
            var decision = MakeFinalDecision(anomalyScore, fraudProbability, riskScore);

            var result = AnalysisResult.Create(
                request.TransactionId,
                anomalyScore,
                fraudProbability,
                riskScore,
                decision);

            foreach (var factor in riskFactors)
            {
                result.AddRiskFactor(factor);
            }

            if (IsHighRiskTransaction(result))
            {
                await PublishHighRiskAlert(request, result, riskFactors);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing transaction {TransactionId}", 
                request.TransactionId);
            return AnalysisResult.CreateFailed(request.TransactionId, ex.Message);
        }
    }
    private async Task PublishHighRiskAlert(
        TransactionRequest request, 
        AnalysisResult result, 
        List<RiskFactor> riskFactors)
    {
        await _publisher.Publish(new HighRiskTransactionDetectedEvent(
            request.TransactionId,
            request.TransactionData.UserId,
            result.RiskScore,
            riskFactors.Select(f => f.Description).ToList()));
    }

    private async Task<double> DetectAnomaliesAsync(FeatureSet features)
    {
        var prediction = await _modelService.PredictAsync(
            "AnomalyDetection_PCA",
            new ModelInput { Features = features.ToVector() });

        return prediction.Score;
    }

    private async Task<double> ClassifyTransactionAsync(FeatureSet features)
    {
        var prediction = await _modelService.PredictAsync(
            "FraudDetection_LightGBM",
            new ModelInput { Features = features.ToVector() });

        return prediction.Probability;
    }

    private DecisionType MakeFinalDecision(
        double anomalyScore, 
        double fraudProbability, 
        RiskScore riskScore)
    {
        if (riskScore.Level == RiskLevel.Critical || 
            fraudProbability > 0.9 || 
            anomalyScore > 0.95)
        {
            return DecisionType.Block;
        }

        if (riskScore.Level == RiskLevel.High || 
            fraudProbability > 0.7 || 
            anomalyScore > 0.85)
        {
            return DecisionType.ReviewRequired;
        }

        return DecisionType.Allow;
    }

    private bool IsHighRiskTransaction(AnalysisResult result)
    {
        return result.RiskScore?.Level >= RiskLevel.High ||
               result.FraudProbability > 0.8 ||
               result.AnomalyScore > 0.9;
    }

    private bool ValidateFraudRules(List<FraudRule> rules)
    {
        if (rules == null || !rules.Any())
        {
            _logger.LogWarning("No rules provided for validation");
            return false;
        }

        foreach (var rule in rules)
        {
            if (!IsValidRule(rule))
            {
                _logger.LogWarning("Invalid rule detected: {RuleId}", rule.RuleId);
                return false;
            }
        }

        return true;
    }

    private bool IsValidRule(FraudRule rule)
    {
        return !string.IsNullOrEmpty(rule.RuleId) &&
               !string.IsNullOrEmpty(rule.Condition) &&
               rule.Action != null;
    }

    private async Task UpdateRule(FraudRule rule)
    {
        var parsedRule = RuleParser.Parse(rule.Condition);
        if (!parsedRule.IsValid)
        {
            throw new Exception( "Invalid rule condition");
        }

        await _transactionRepository.UpdateFraudRuleAsync(rule);
        _logger.LogInformation("Updated fraud rule: {RuleId}", rule.RuleId);
    }
}