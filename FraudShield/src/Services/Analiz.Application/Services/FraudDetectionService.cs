using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Analiz.Application.Services
{
    public class FraudDetectionService : IFraudDetectionService
    {
        private readonly IModelService _modelService;
        private readonly IFeatureExtractionService _featureExtractor;
        private readonly IFraudRuleEngine _ruleEngine;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAnalysisResultRepository _analysisRepository;
        private readonly IFraudRuleRepository _ruleRepository;
        private readonly IFraudAlertRepository _alertRepository;
        private readonly ILogger<FraudDetectionService> _logger;
        
        // Threshold configurations
        private const double HIGH_RISK_THRESHOLD = 0.8;
        private const double MEDIUM_RISK_THRESHOLD = 0.5;
        private const double ANOMALY_THRESHOLD = 2.5;
        
        public FraudDetectionService(
            IModelService modelService,
            IFeatureExtractionService featureExtractor,
            IFraudRuleEngine ruleEngine,
            ITransactionRepository transactionRepository,
            IAnalysisResultRepository analysisRepository,
            IFraudRuleRepository ruleRepository,
            IFraudAlertRepository alertRepository,
            ILogger<FraudDetectionService> logger)
        {
            _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
            _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
            _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
            _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
            _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AnalysisResult> AnalyzeTransactionAsync(TransactionRequest request)
        {
            try
            {
                _logger.LogInformation("Analyzing transaction {TransactionId}", request.TransactionId);
                
                // 1. Store the transaction
                await _transactionRepository.AddAsync(
                    Transaction.Create(
                        request.TransactionData.TransactionId,
                        request.TransactionData.UserId,
                        request.TransactionData.Amount,
                        request.TransactionData.MerchantId,
                        request.TransactionData.Timestamp,
                        request.TransactionData.Type,
                        request.TransactionData.Location));
                
                // 2. Perform ML-based risk evaluation
                var riskEvaluation = await EvaluateRiskAsync(request.TransactionData);
                
                // 3. Apply fraud rules
                var ruleResults = await _ruleEngine.EvaluateRulesAsync(request.TransactionData);
                
                // 4. Combine ML and rule-based evaluations
                var finalDecision = DetermineDecision(riskEvaluation, ruleResults);
                
                // 5. Create analysis result
                var analysisResult = AnalysisResult.Create(
                    request.TransactionId,
                    riskEvaluation.AnomalyScore,
                    riskEvaluation.FraudProbability,
                    riskEvaluation.RiskScore,
                    finalDecision);
                
                // 6. Add risk factors
                foreach (var factor in riskEvaluation.RiskFactors)
                {
                    analysisResult.AddRiskFactor(factor);
                }
                
                // Add rule-based risk factors
                foreach (var ruleResult in ruleResults.Where(r => r.IsTriggered))
                {
                    analysisResult.AddRiskFactor(RiskFactor.Create(
                        RiskFactorType.RuleViolation,
                        $"Rule violated: {ruleResult.RuleName}",
                        ruleResult.Confidence));
                }
                
                // 7. Create alerts if needed
                if (finalDecision == DecisionType.Deny || finalDecision == DecisionType.ReviewRequired)
                {
                    await CreateFraudAlertAsync(request.TransactionData, analysisResult);
                }
                
                // 8. Save analysis result
                await _analysisRepository.AddAsync(analysisResult);
                
                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing transaction {TransactionId}", request.TransactionId);
                return AnalysisResult.CreateFailed(request.TransactionId, ex.Message);
            }
        }

        public async Task<List<AnalysisResult>> AnalyzeBatchAsync(List<TransactionRequest> requests)
        {
            var results = new List<AnalysisResult>();
            
            foreach (var request in requests)
            {
                var result = await AnalyzeTransactionAsync(request);
                results.Add(result);
            }
            
            return results;
        }

        public async Task<AnalysisResult> GetAnalysisResultAsync(Guid analysisId)
        {
            return await _analysisRepository.GetByIdAsync(analysisId);
        }

        public async Task<RiskEvaluation> EvaluateRiskAsync(TransactionData data)
        {
            try
            {
                // 1. Extract features
                var features = await _featureExtractor.ExtractFeaturesAsync(data, ModelType.Ensemble);
                
                // 2. Create model input
                var modelInput = new ModelInput
                {
                    Features = features.ToVector()
                };
                
                // 3. Get LightGBM prediction (supervised - probability based)
                var lightGbmTransformer = await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_LightGBM");
                var lightGbmPrediction = _modelService.PredictSingle(lightGbmTransformer, modelInput);
                
                // 4. Get PCA prediction (unsupervised - anomaly based)
                var pcaTransformer = await _modelService.GetModelTransformerAsync("CreditCard_AnomalyDetection_PCA");
                var pcaPrediction = _modelService.PredictSingle(pcaTransformer, modelInput);
                
                // 5. Get ensemble prediction (if available)
                double ensembleProbability = 0;
                try
                {
                    var ensembleTransformer = await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_Ensemble");
                    var ensemblePrediction = _modelService.PredictSingle(ensembleTransformer, modelInput);
                    ensembleProbability = ensemblePrediction.Probability;
                }
                catch
                {
                    // If ensemble model is not available, use weighted average
                    ensembleProbability = lightGbmPrediction.Probability * 0.7 + (pcaPrediction.AnomalyScore > ANOMALY_THRESHOLD ? 1.0 : 0.0) * 0.3;
                }
                
                // 6. Determine risk score
                var riskScore = DetermineRiskScore(
                    lightGbmProbability: lightGbmPrediction.Probability,
                    anomalyScore: pcaPrediction.AnomalyScore,
                    ensembleProbability: ensembleProbability);
                
                // 7. Identify risk factors
                var riskFactors = IdentifyRiskFactors(data, lightGbmPrediction, pcaPrediction);
                
                return new RiskEvaluation
                {
                    FraudProbability = ensembleProbability,
                    AnomalyScore = pcaPrediction.AnomalyScore,
                    RiskScore = riskScore,
                    RiskFactors = riskFactors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating risk for transaction");
                throw new InvalidOperationException("Failed to evaluate transaction risk", ex);
            }
        }

        public async Task<bool> UpdateFraudRulesAsync(List<FraudRule> rules)
        {
            try
            {
                // Get existing rules
                var existingRules = await _ruleRepository.GetAllAsync();
                
                foreach (var rule in rules)
                {
                    var existingRule = existingRules.FirstOrDefault(r => r.RuleId == rule.RuleId);
                    
                    if (existingRule != null)
                    {
                        // Update existing rule
                        existingRule.Update(rule);
                        await _ruleRepository.UpdateAsync(existingRule);
                    }
                    else
                    {
                        // Add new rule
                        await _ruleRepository.AddAsync(rule);
                    }
                }
                
                // Reload rules in the rule engine
                await _ruleEngine.ReloadRulesAsync();
                
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
            return await _alertRepository.GetActiveAlertsAsync();
        }
        
        #region Helper Methods
        
        private RiskScore DetermineRiskScore(double lightGbmProbability, double anomalyScore, double ensembleProbability)
        {
            // Weighted determination based on multiple signals
            if (ensembleProbability > HIGH_RISK_THRESHOLD || 
                (lightGbmProbability > HIGH_RISK_THRESHOLD && anomalyScore > ANOMALY_THRESHOLD))
            {
                return RiskScore.High;
            }
            
            if (ensembleProbability > MEDIUM_RISK_THRESHOLD || 
                (lightGbmProbability > MEDIUM_RISK_THRESHOLD && anomalyScore > ANOMALY_THRESHOLD * 0.7))
            {
                return RiskScore.Medium;
            }
            
            return RiskScore.Low;
        }
        
        private List<RiskFactor> IdentifyRiskFactors(
            TransactionData data, 
            ModelPrediction lightGbmPrediction, 
            ModelPrediction pcaPrediction)
        {
            var factors = new List<RiskFactor>();
            
            // Extract top contributing features from the ML model
            if (lightGbmPrediction.Metadata.TryGetValue("TopFeatures", out var topFeaturesObj))
            {
                string[] topFeatures = topFeaturesObj.ToString().Split(',');
                foreach (var feature in topFeatures)
                {
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.ModelFeature,
                        $"Contributing feature: {feature}",
                        lightGbmPrediction.Probability));
                }
            }
            
            // Add anomaly detection as a risk factor if significant
            if (pcaPrediction.AnomalyScore > ANOMALY_THRESHOLD)
            {
                factors.Add(RiskFactor.Create(
                    RiskFactorType.AnomalyDetection,
                    $"Unusual transaction pattern detected (score: {pcaPrediction.AnomalyScore:F2})",
                    Math.Min(pcaPrediction.AnomalyScore / (ANOMALY_THRESHOLD * 2), 1.0)));
            }
            
            // Check for amount-related risks
            if (data.Amount > 1000)
            {
                factors.Add(RiskFactor.Create(
                    RiskFactorType.HighValue,
                    $"High value transaction (${data.Amount})",
                    Math.Min((double)data.Amount / 10000, 1.0)));
            }
            
            // Add location-based risks if applicable
            if (data.Location != null && data.Location.IsHighRiskRegion)
            {
                factors.Add(RiskFactor.Create(
                    RiskFactorType.Location,
                    $"Transaction from high-risk region ({data.Location.Country})",
                    0.7));
            }
            
            return factors;
        }
        
        private DecisionType DetermineDecision(
            RiskEvaluation riskEvaluation, 
            IEnumerable<RuleResult> ruleResults)
        {
            // Check for rule-enforced decisions first
            var blockRule = ruleResults.FirstOrDefault(r => 
                r.IsTriggered && r.Action == RuleAction.Block);
                
            if (blockRule != null)
            {
                return DecisionType.Deny;
            }
            
            var reviewRule = ruleResults.FirstOrDefault(r => 
                r.IsTriggered && r.Action == RuleAction.Review);
                
            if (reviewRule != null)
            {
                return DecisionType.ReviewRequired;
            }
            
            // If no rules enforced a decision, use ML-based risk evaluation
            switch (riskEvaluation.RiskScore)
            {
                case RiskScore.High:
                    return DecisionType.Deny;
                    
                case RiskScore.Medium:
                    return DecisionType.ReviewRequired;
                    
                default:
                    return DecisionType.Approve;
            }
        }
        
        private async Task CreateFraudAlertAsync(TransactionData data, AnalysisResult result)
        {
            var alert = FraudAlert.Create(
                data.TransactionId,
                result.Id,
                result.RiskScore == RiskScore.High ? AlertSeverity.High : AlertSeverity.Medium,
                $"Potential fraud detected for transaction {data.TransactionId}",
                GenerateAlertDetails(data, result));
                
            await _alertRepository.AddAsync(alert);
        }
        
        private string GenerateAlertDetails(TransactionData data, AnalysisResult result)
        {
            var details = new System.Text.StringBuilder();
            
            details.AppendLine($"Transaction ID: {data.TransactionId}");
            details.AppendLine($"Amount: ${data.Amount}");
            details.AppendLine($"User ID: {data.UserId}");
            details.AppendLine($"Merchant: {data.MerchantId}");
            details.AppendLine($"Timestamp: {data.Timestamp}");
            details.AppendLine($"Fraud Probability: {result.FraudProbability:P2}");
            details.AppendLine($"Anomaly Score: {result.AnomalyScore:F2}");
            details.AppendLine($"Risk Score: {result.RiskScore}");
            details.AppendLine($"Decision: {result.Decision}");
            details.AppendLine("Risk Factors:");
            
            foreach (var factor in result.RiskFactors)
            {
                details.AppendLine($" - {factor.Description} (Confidence: {factor.Confidence:P2})");
            }
            
            return details.ToString();
        }
        
        #endregion
    }
}