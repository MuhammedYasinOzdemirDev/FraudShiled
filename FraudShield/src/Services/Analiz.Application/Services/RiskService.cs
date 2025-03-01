using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Analiz.Application.Services;

 public class RiskService : IRiskService
    {
        private readonly IModelService _modelService;
        private readonly IFeatureExtractionService _featureExtractor;
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<RiskService> _logger;
        private readonly RiskSettings _settings;

        // Eşik değer konfigürasyonları
        private const double HIGH_RISK_THRESHOLD = 0.8;
        private const double MEDIUM_RISK_THRESHOLD = 0.5;
        private const double ANOMALY_THRESHOLD = 2.5;

        public RiskService(
            IModelService modelService,
            IFeatureExtractionService featureExtractor,
            ITransactionRepository transactionRepository,
            IOptions<RiskSettings> settings,
            ILogger<RiskService> logger)
        {
            _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
            _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
            _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RiskEvaluation> EvaluateRiskAsync(TransactionData data)
        {
            try
            {
                _logger.LogInformation("İşlem risk değerlendirmesi başlatılıyor: {TransactionId}", data.TransactionId);

                // 1. Özellikleri çıkart
                var features = await _featureExtractor.ExtractFeaturesAsync(data, ModelType.Ensemble);
                
                // 2. Model girişi oluştur
                var modelInput = new ModelInput
                {
                    Features = features.ToVector()
                };
                
                // 3. LightGBM tahmini al (gözetimli - olasılık bazlı)
                var lightGbmTransformer = await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_LightGBM");
                var lightGbmPrediction = _modelService.PredictSingle(lightGbmTransformer, modelInput);
                
                // 4. PCA tahmini al (gözetimsiz - anomali bazlı)
                var pcaTransformer = await _modelService.GetModelTransformerAsync("CreditCard_AnomalyDetection_PCA");
                var pcaPrediction = _modelService.PredictSingle(pcaTransformer, modelInput);
                
                // 5. Ensemble tahmini al (eğer mevcutsa)
                double ensembleProbability = 0;
                try
                {
                    var ensembleTransformer = await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_Ensemble");
                    var ensemblePrediction = _modelService.PredictSingle(ensembleTransformer, modelInput);
                    ensembleProbability = ensemblePrediction.Probability;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ensemble model kullanılamıyor, ağırlıklı ortalama kullanılacak");
                    // Ensemble model mevcut değilse ağırlıklı ortalama kullan
                    ensembleProbability = lightGbmPrediction.Probability * 0.7 + (pcaPrediction.AnomalyScore > ANOMALY_THRESHOLD ? 1.0 : 0.0) * 0.3;
                }
                
                // 6. Risk skorunu belirle
                var riskScore = await DetermineRiskScoreAsync(
                    lightGbmProbability: lightGbmPrediction.Probability,
                    anomalyScore: pcaPrediction.AnomalyScore);
                
                // 7. Risk faktörlerini belirle
                var riskFactors = await IdentifyRiskFactorsAsync(data, lightGbmPrediction);
                
                // 8. Özellik önem değerlerini hesapla
                var featureImportance = await CalculateFeatureImportanceAsync(data, lightGbmPrediction);
                
                // 9. Özellik değerleri sözlüğünü oluştur
                var featureValues = new Dictionary<string, double>();
                foreach (var feature in features.FeatureNames)
                {
                    featureValues[feature] = features[feature];
                }
                
                return new RiskEvaluation
                {
                    FraudProbability = ensembleProbability,
                    AnomalyScore = pcaPrediction.AnomalyScore,
                    RiskScore = riskScore,
                    RiskFactors = riskFactors,
                    FeatureValues = featureValues,
                    FeatureImportance = featureImportance
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İşlem risk değerlendirmesinde hata: {TransactionId}", data.TransactionId);
                throw new InvalidOperationException("İşlem riski değerlendirilirken hata oluştu", ex);
            }
        }

        public async Task<RiskScore> DetermineRiskScoreAsync(double fraudProbability, double anomalyScore)
        {
            // Çoklu sinyallere dayalı ağırlıklı karar
            if (fraudProbability > _settings.HighRiskThreshold || 
                anomalyScore > _settings.AnomalyThreshold)
            {
                return RiskScore.High;
            }
            
            if (fraudProbability > _settings.MediumRiskThreshold || 
                anomalyScore > _settings.AnomalyThreshold * 0.7)
            {
                return RiskScore.Medium;
            }
            
            return RiskScore.Low;
        }

        public async Task<bool> IsHighRiskTransactionAsync(TransactionData data)
        {
            var evaluation = await EvaluateRiskAsync(data);
            return evaluation.RiskScore == RiskScore.High;
        }

        public async Task<List<RiskFactor>> IdentifyRiskFactorsAsync(TransactionData data, ModelPrediction prediction)
        {
            var factors = new List<RiskFactor>();
            
            // ML modelinden önemli özellikleri çıkart
            if (prediction.Metadata != null && prediction.Metadata.TryGetValue("TopFeatures", out var topFeaturesObj))
            {
                string[] topFeatures = topFeaturesObj.ToString().Split(',');
                foreach (var feature in topFeatures)
                {
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.ModelFeature,
                        $"Katkıda bulunan özellik: {feature}",
                        prediction.Probability));
                }
            }
            
            // Yüksek tutarlı işlem kontrolü
            if (data.Amount > _settings.HighValueThreshold)
            {
                factors.Add(RiskFactor.Create(
                    RiskFactorType.HighValue,
                    $"Yüksek tutarlı işlem (₺{data.Amount})",
                    Math.Min((double)data.Amount / (_settings.HighValueThreshold * 2), 1.0)));
            }
            
            // Kullanıcının işlem geçmişi kontrolü
            try
            {
                var userProfile = await GetUserRiskProfileAsync(data.UserId);
                
                // Kullanıcı geçmişiyle karşılaştırarak olağandışı işlem tutarı kontrolü
                if (data.Amount > userProfile.AverageTransactionAmount * 3)
                {
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.UnusualAmount,
                        $"İşlem tutarı kullanıcı ortalamasından {data.Amount / userProfile.AverageTransactionAmount:F1}x daha yüksek",
                        Math.Min((double)data.Amount / (userProfile.AverageTransactionAmount * 5), 1.0)));
                }
                
                // İşlem sıklığı anomali kontrolü
                if (userProfile.TransactionCount > 0 && userProfile.DaysSinceLastTransaction < 1)
                {
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.FrequencyAnomaly,
                        "Kısa süre içinde birden fazla işlem",
                        0.6));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Risk faktörleri için kullanıcı geçmişi kontrol edilemedi");
            }
            
            // Lokasyon bazlı riskler (eğer varsa)
            if (data.Location != null && data.Location.IsHighRiskRegion)
            {
                factors.Add(RiskFactor.Create(
                    RiskFactorType.Location,
                    $"Yüksek riskli bölgeden işlem ({data.Location.Country})",
                    0.7));
            }
            
            // Cihaz/Tarayıcı risk faktörleri
            if (data.DeviceInfo != null)
            {
                if (data.DeviceInfo.IpChanged)
                {
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.DeviceAnomaly,
                        "Son işlemden farklı IP adresi",
                        0.65));
                }
                
                if (data.DeviceInfo.IsNewDevice)
                {
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.DeviceAnomaly,
                        "Yeni cihazdan işlem",
                        0.5));
                }
            }
            
            return factors;
        }

        public async Task<Dictionary<string, double>> CalculateFeatureImportanceAsync(TransactionData data, ModelPrediction prediction)
        {
            // Gerçek bir uygulamada, modele özgü yöntemler kullanarak özellik önemini alırsınız
            // LightGBM için SHAP değerleri veya yerleşik özellik önemi kullanılabilir
            
            // Bu basitleştirilmiş bir örnek
            var importance = new Dictionary<string, double>();
            
            // Örnek önem değerleri
            importance["Amount"] = 0.32;
            importance["Location"] = 0.21;
            importance["TimeOfDay"] = 0.15;
            importance["TransactionType"] = 0.12;
            importance["UserHistory"] = 0.10;
            importance["DeviceInfo"] = 0.08;
            importance["Other"] = 0.02;
            
            return importance;
        }

        public async Task<RiskProfile> GetUserRiskProfileAsync(string userId)
        {
            // Kullanıcının önceki işlemlerini al
            var userTransactions = await _transactionRepository.GetUserTransactionsAsync(userId, 90); // Son 90 gün
            
            if (userTransactions.Count == 0)
            {
                return new RiskProfile
                {
                    UserId = userId,
                    TransactionCount = 0,
                    AverageTransactionAmount = 0,
                    MaxTransactionAmount = 0,
                    DaysSinceLastTransaction = double.MaxValue,
                    PreviousFraudCount = 0,
                    RiskLevel = RiskScore.Medium // Yeni kullanıcılar için varsayılan
                };
            }
            
            var lastTransaction = userTransactions.OrderByDescending(t => t.Timestamp).First();
            var daysSinceLastTransaction = (DateTime.UtcNow - lastTransaction.Timestamp).TotalDays;
            
            return new RiskProfile
            {
                UserId = userId,
                TransactionCount = userTransactions.Count,
                AverageTransactionAmount = userTransactions.Average(t => (double)t.Amount),
                MaxTransactionAmount = userTransactions.Max(t => (double)t.Amount),
                DaysSinceLastTransaction = daysSinceLastTransaction,
                PreviousFraudCount = userTransactions.Count(t => t.IsFraudulent),
                RiskLevel = DetermineUserRiskLevel(userTransactions)
            };
        }

        private RiskScore DetermineUserRiskLevel(List<Transaction> transactions)
        {
            var fraudRatio = (double)transactions.Count(t => t.IsFraudulent) / transactions.Count;
            
            if (fraudRatio > 0.05) return RiskScore.High;
            if (fraudRatio > 0.01) return RiskScore.Medium;
            return RiskScore.Low;
        }
    }

    public class RiskProfile
    {
        public string UserId { get; set; }
        public int TransactionCount { get; set; }
        public double AverageTransactionAmount { get; set; }
        public double MaxTransactionAmount { get; set; }
        public double DaysSinceLastTransaction { get; set; }
        public int PreviousFraudCount { get; set; }
        public RiskScore RiskLevel { get; set; }
    }

    public class RiskSettings
    {
        public double HighRiskThreshold { get; set; } = 0.8;
        public double MediumRiskThreshold { get; set; } = 0.5;
        public double AnomalyThreshold { get; set; } = 2.5;
        public decimal HighValueThreshold { get; set; } = 1000;
    }
}