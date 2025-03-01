using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

public class AlertService : IAlertService
    {
        private readonly IFraudAlertRepository _alertRepository;
        private readonly ILogger<AlertService> _logger;
        private readonly INotificationService _notificationService;

        public AlertService(
            IFraudAlertRepository alertRepository,
            INotificationService notificationService,
            ILogger<AlertService> logger)
        {
            _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FraudAlert> CreateAlertAsync(TransactionData transaction, AnalysisResult analysisResult, AlertSeverity severity)
        {
            try
            {
                _logger.LogInformation("Dolandırıcılık uyarısı oluşturuluyor: {TransactionId}", transaction.TransactionId);

                // Uyarı detaylarını oluştur
                var details = GenerateAlertDetails(transaction, analysisResult);

                // Uyarı varlığını oluştur
                var alert = FraudAlert.Create(
                    transaction.TransactionId,
                    analysisResult.Id,
                    severity,
                    $"İşlem için potansiyel dolandırıcılık tespit edildi: {transaction.TransactionId}",
                    details);

                // Risk faktörlerini uyarıya ekle
                foreach (var factor in analysisResult.RiskFactors)
                {
                    alert.AddRiskFactor(factor);
                }

                // Veritabanına kaydet
                await _alertRepository.AddAsync(alert);

                // Önceliğe göre bildirim gönder
                if (severity == AlertSeverity.High)
                {
                    await _notificationService.SendHighPriorityAlertAsync(alert);
                }
                else if (severity == AlertSeverity.Medium)
                {
                    await _notificationService.SendAlertNotificationAsync(alert);
                }

                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarı oluştururken hata: {TransactionId}", transaction.TransactionId);
                throw;
            }
        }

        public async Task<List<FraudAlert>> GetActiveAlertsAsync()
        {
            try
            {
                return await _alertRepository.GetActiveAlertsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aktif uyarıları alırken hata");
                throw;
            }
        }

        public async Task<List<FraudAlert>> GetAlertsByUserIdAsync(string userId)
        {
            try
            {
                return await _alertRepository.GetByUserIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı için uyarıları alırken hata: {UserId}", userId);
                throw;
            }
        }

        public async Task<FraudAlert> GetAlertByIdAsync(Guid alertId)
        {
            try
            {
                return await _alertRepository.GetByIdAsync(alertId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarıyı getirirken hata: {AlertId}", alertId);
                throw;
            }
        }

        public async Task<FraudAlert> ResolveAlertAsync(Guid alertId, string resolution, string resolvedBy)
        {
            try
            {
                var alert = await _alertRepository.GetByIdAsync(alertId);
                if (alert == null)
                {
                    throw new KeyNotFoundException($"{alertId} ID'li uyarı bulunamadı");
                }

                alert.Resolve(resolution, resolvedBy);
                await _alertRepository.UpdateAsync(alert);
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarıyı çözümlerken hata: {AlertId}", alertId);
                throw;
            }
        }

        public async Task<FraudAlert> AssignAlertAsync(Guid alertId, string assignedTo)
        {
            try
            {
                var alert = await _alertRepository.GetByIdAsync(alertId);
                if (alert == null)
                {
                    throw new KeyNotFoundException($"{alertId} ID'li uyarı bulunamadı");
                }

                alert.Assign(assignedTo);
                await _alertRepository.UpdateAsync(alert);
                
                // Atanan kişiye bildirim gönder
                await _notificationService.SendAlertAssignmentAsync(alert, assignedTo);
                
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarıyı atarken hata: {AlertId}, {AssignedTo}", alertId, assignedTo);
                throw;
            }
        }

        public async Task<List<FraudAlert>> GetAlertsByStatusAsync(AlertStatus status)
        {
            try
            {
                return await _alertRepository.GetByStatusAsync(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Status} durumundaki uyarıları alırken hata", status);
                throw;
            }
        }

        public async Task<List<FraudAlert>> GetAlertsBySeverityAsync(AlertSeverity severity)
        {
            try
            {
                return await _alertRepository.GetBySeverityAsync(severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Severity} önceliğindeki uyarıları alırken hata", severity);
                throw;
            }
        }

        public async Task<AlertSummary> GetAlertSummaryAsync()
        {
            try
            {
                var allAlerts = await _alertRepository.GetAllAsync();
                
                var summary = new AlertSummary
                {
                    TotalAlerts = allAlerts.Count,
                    ActiveAlerts = allAlerts.Count(a => a.Status != AlertStatus.Resolved),
                    ResolvedAlerts = allAlerts.Count(a => a.Status == AlertStatus.Resolved),
                    HighSeverity = allAlerts.Count(a => a.Severity == AlertSeverity.High),
                    MediumSeverity = allAlerts.Count(a => a.Severity == AlertSeverity.Medium),
                    LowSeverity = allAlerts.Count(a => a.Severity == AlertSeverity.Low),
                    PendingReview = allAlerts.Count(a => a.Status == AlertStatus.PendingReview),
                    AlertsByCategory = allAlerts
                        .GroupBy(a => a.AlertType.ToString())
                        .ToDictionary(g => g.Key, g => g.Count())
                };
                
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarı özeti oluştururken hata");
                throw;
            }
        }

        private string GenerateAlertDetails(TransactionData data, AnalysisResult result)
        {
            var details = new System.Text.StringBuilder();
            
            details.AppendLine($"İşlem ID: {data.TransactionId}");
            details.AppendLine($"Tutar: ₺{data.Amount}");
            details.AppendLine($"Kullanıcı ID: {data.UserId}");
            details.AppendLine($"Satıcı: {data.MerchantId}");
            details.AppendLine($"Tarih: {data.Timestamp}");
            details.AppendLine($"İşlem Tipi: {data.Type}");
            
            if (data.Location != null)
            {
                details.AppendLine($"Konum: {data.Location.City}, {data.Location.Country}");
            }
            
            if (data.DeviceInfo != null)
            {
                details.AppendLine($"Cihaz: {data.DeviceInfo.DeviceId}");
                details.AppendLine($"IP Adresi: {data.DeviceInfo.IpAddress}");
            }
            
            details.AppendLine();
            details.AppendLine($"Dolandırıcılık Olasılığı: {result.FraudProbability:P2}");
            details.AppendLine($"Anomali Skoru: {result.AnomalyScore:F2}");
            details.AppendLine($"Risk Skoru: {result.RiskScore}");
            details.AppendLine($"Karar: {result.Decision}");
            details.AppendLine($"Analiz Tarihi: {result.AnalyzedAt}");
            
            details.AppendLine();
            details.AppendLine("Risk Faktörleri:");
            
            foreach (var factor in result.RiskFactors)
            {
                details.AppendLine($" - {factor.Description} (Güven: {factor.Confidence:P2})");
            }
            
            return details.ToString();
        }
    }