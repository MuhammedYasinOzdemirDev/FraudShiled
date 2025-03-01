using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analiz.Infrastructure.Services
{
    /// <summary>
    /// Dolandırıcılık tespit sistemi bildirim servisi implementasyonu
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IPushNotificationService _pushService;
        private readonly INotificationChannelRepository _channelRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITemplateService _templateService;
        private readonly ILogger<NotificationService> _logger;
        private readonly NotificationSettings _settings;

        public NotificationService(
            IEmailService emailService,
            ISmsService smsService,
            IPushNotificationService pushService,
            INotificationChannelRepository channelRepository,
            IUserRepository userRepository,
            ITemplateService templateService,
            IOptions<NotificationSettings> settings,
            ILogger<NotificationService> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _pushService = pushService ?? throw new ArgumentNullException(nameof(pushService));
            _channelRepository = channelRepository ?? throw new ArgumentNullException(nameof(channelRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<bool> SendAlertNotificationAsync(FraudAlert alert)
        {
            try
            {
                _logger.LogInformation("Standart uyarı bildirimi gönderiliyor: {AlertId}", alert.Id);

                // 1. Şablonları hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("alert_email");
                var smsTemplate = await _templateService.GetTemplateAsync("alert_sms");
                var pushTemplate = await _templateService.GetTemplateAsync("alert_push");

                // 2. Bildirim alıcılarını belirle
                var recipients = await DetermineRecipientsAsync(alert);

                // 3. Her bir alıcı için bildirim gönder
                var tasks = new List<Task<bool>>();

                foreach (var userId in recipients)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null) continue;

                    var preferences = await GetUserNotificationPreferencesAsync(userId);
                    
                    // Eğer kullanıcı bu öncelikteki uyarıları almak istemiyorsa atla
                    if ((int)alert.Severity < (int)preferences.MinimumAlertSeverity)
                        continue;

                    // Email bildirimi
                    if (preferences.EmailEnabled && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailContent = FormatEmailContent(emailTemplate, alert, user.Name);
                        tasks.Add(_emailService.SendEmailAsync(
                            user.Email,
                            $"Dolandırıcılık Uyarısı: {alert.Title}",
                            emailContent));
                    }

                    // SMS bildirimi
                    if (preferences.SmsEnabled && !string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        var smsContent = FormatSmsContent(smsTemplate, alert);
                        tasks.Add(_smsService.SendSmsAsync(user.PhoneNumber, smsContent));
                    }

                    // Push bildirimi
                    if (preferences.PushEnabled && !string.IsNullOrEmpty(user.DeviceToken))
                    {
                        var pushContent = FormatPushContent(pushTemplate, alert);
                        tasks.Add(_pushService.SendPushNotificationAsync(
                            user.DeviceToken,
                            "Dolandırıcılık Uyarısı",
                            pushContent));
                    }
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Uyarı bildirimi tamamlandı: {AlertId}, {SuccessCount}/{TotalCount} başarılı",
                    alert.Id, results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarı bildirimi gönderilirken hata oluştu: {AlertId}", alert.Id);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendHighPriorityAlertAsync(FraudAlert alert)
        {
            try
            {
                _logger.LogInformation("Yüksek öncelikli uyarı bildirimi gönderiliyor: {AlertId}", alert.Id);

                // 1. Acil durum şablonlarını hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("urgent_alert_email");
                var smsTemplate = await _templateService.GetTemplateAsync("urgent_alert_sms");
                var pushTemplate = await _templateService.GetTemplateAsync("urgent_alert_push");

                // 2. Yüksek öncelikli uyarı alıcılarını belirle (genellikle yöneticiler ve güvenlik ekibi)
                var recipients = await DetermineHighPriorityRecipientsAsync();

                // 3. Birden fazla bildirim kanalını paralel kullan
                var tasks = new List<Task<bool>>();

                foreach (var userId in recipients)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null) continue;

                    // Yüksek öncelikli uyarılarda kullanıcı tercihleri bypass edilebilir
                    // ancak yine de kontrol edelim
                    var preferences = await GetUserNotificationPreferencesAsync(userId);

                    // Email her durumda gönderilir
                    if (!string.IsNullOrEmpty(user.Email))
                    {
                        var emailContent = FormatEmailContent(emailTemplate, alert, user.Name);
                        tasks.Add(_emailService.SendEmailAsync(
                            user.Email,
                            $"ACİL: Yüksek Riskli Dolandırıcılık Uyarısı - {alert.Title}",
                            emailContent,
                            isHighPriority: true));
                    }

                    // SMS genellikle acil durumlarda tercih edilir
                    if (!string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        var smsContent = FormatSmsContent(smsTemplate, alert);
                        tasks.Add(_smsService.SendSmsAsync(user.PhoneNumber, smsContent, 
                            isUrgent: true));
                    }

                    // Push bildirimi (kritik bildirim olarak işaretlenir)
                    if (!string.IsNullOrEmpty(user.DeviceToken))
                    {
                        var pushContent = FormatPushContent(pushTemplate, alert);
                        tasks.Add(_pushService.SendPushNotificationAsync(
                            user.DeviceToken,
                            "ACİL: Dolandırıcılık Uyarısı",
                            pushContent,
                            isCritical: true));
                    }
                }

                // Eğer özel bir acil durum kanalı varsa, onu da kullan
                if (!string.IsNullOrEmpty(_settings.EmergencyWebhookUrl))
                {
                    tasks.Add(SendEmergencyWebhookAsync(alert));
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Yüksek öncelikli uyarı bildirimi tamamlandı: {AlertId}, {SuccessCount}/{TotalCount} başarılı",
                    alert.Id, results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yüksek öncelikli uyarı bildirimi gönderilirken hata oluştu: {AlertId}", alert.Id);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendAlertAssignmentAsync(FraudAlert alert, string assignedTo)
        {
            try
            {
                _logger.LogInformation("Uyarı atama bildirimi gönderiliyor: {AlertId}, Atanan: {AssignedTo}", 
                    alert.Id, assignedTo);

                // 1. Atama şablonlarını hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("alert_assignment_email");
                var pushTemplate = await _templateService.GetTemplateAsync("alert_assignment_push");

                // 2. Atanan kullanıcıyı al
                var user = await _userRepository.GetByIdAsync(assignedTo);
                if (user == null)
                {
                    _logger.LogWarning("Atanan kullanıcı bulunamadı: {UserId}", assignedTo);
                    return false;
                }

                var preferences = await GetUserNotificationPreferencesAsync(assignedTo);
                var tasks = new List<Task<bool>>();

                // 3. Email bildirimi
                if (preferences.EmailEnabled && !string.IsNullOrEmpty(user.Email))
                {
                    var emailContent = FormatAssignmentEmailContent(emailTemplate, alert, user.Name);
                    tasks.Add(_emailService.SendEmailAsync(
                        user.Email,
                        $"Size atanan dolandırıcılık uyarısı: {alert.Title}",
                        emailContent));
                }

                // 4. Push bildirimi
                if (preferences.PushEnabled && !string.IsNullOrEmpty(user.DeviceToken))
                {
                    var pushContent = FormatAssignmentPushContent(pushTemplate, alert);
                    tasks.Add(_pushService.SendPushNotificationAsync(
                        user.DeviceToken,
                        "Size atanan dolandırıcılık uyarısı",
                        pushContent));
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Uyarı atama bildirimi tamamlandı: {AlertId}, {SuccessCount}/{TotalCount} başarılı",
                    alert.Id, results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarı atama bildirimi gönderilirken hata oluştu: {AlertId}, {AssignedTo}", 
                    alert.Id, assignedTo);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendAlertResolutionAsync(FraudAlert alert, string resolvedBy)
        {
            try
            {
                _logger.LogInformation("Uyarı çözüm bildirimi gönderiliyor: {AlertId}, Çözen: {ResolvedBy}", 
                    alert.Id, resolvedBy);

                // 1. Çözüm şablonlarını hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("alert_resolution_email");
                var pushTemplate = await _templateService.GetTemplateAsync("alert_resolution_push");

                // 2. İlgili kullanıcıları belirle (genellikle uyarı sahibi, atanan kişi ve yöneticiler)
                var recipients = await DetermineResolutionRecipientsAsync(alert);
                var tasks = new List<Task<bool>>();
                
                foreach (var userId in recipients)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null) continue;

                    var preferences = await GetUserNotificationPreferencesAsync(userId);

                    // Email bildirimi
                    if (preferences.EmailEnabled && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailContent = FormatResolutionEmailContent(emailTemplate, alert, user.Name, resolvedBy);
                        tasks.Add(_emailService.SendEmailAsync(
                            user.Email,
                            $"Dolandırıcılık uyarısı çözüldü: {alert.Title}",
                            emailContent));
                    }

                    // Push bildirimi
                    if (preferences.PushEnabled && !string.IsNullOrEmpty(user.DeviceToken))
                    {
                        var pushContent = FormatResolutionPushContent(pushTemplate, alert, resolvedBy);
                        tasks.Add(_pushService.SendPushNotificationAsync(
                            user.DeviceToken,
                            "Dolandırıcılık uyarısı çözüldü",
                            pushContent));
                    }
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Uyarı çözüm bildirimi tamamlandı: {AlertId}, {SuccessCount}/{TotalCount} başarılı",
                    alert.Id, results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uyarı çözüm bildirimi gönderilirken hata oluştu: {AlertId}, {ResolvedBy}",
                    alert.Id, resolvedBy);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendBulkAlertNotificationAsync(List<FraudAlert> alerts)
        {
            try
            {
                if (alerts == null || !alerts.Any())
                {
                    _logger.LogWarning("Bildirilecek uyarı bulunamadı");
                    return false;
                }

                _logger.LogInformation("Toplu uyarı bildirimi gönderiliyor: {AlertCount} uyarı", alerts.Count);

                // 1. Toplu uyarı şablonlarını hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("bulk_alert_email");
                var smsTemplate = await _templateService.GetTemplateAsync("bulk_alert_sms");

                // 2. Alıcıları belirle (genellikle yöneticiler ve ilgili ekipler)
                var recipients = await DetermineBulkAlertRecipientsAsync();
                var tasks = new List<Task<bool>>();
                
                foreach (var userId in recipients)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null) continue;

                    var preferences = await GetUserNotificationPreferencesAsync(userId);

                    // Email bildirimi (toplu uyarılar için en uygun format)
                    if (preferences.EmailEnabled && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailContent = FormatBulkAlertEmailContent(emailTemplate, alerts, user.Name);
                        tasks.Add(_emailService.SendEmailAsync(
                            user.Email,
                            $"Dolandırıcılık Uyarıları Özeti ({alerts.Count} yeni uyarı)",
                            emailContent));
                    }

                    // SMS bildirimi (kısa özet)
                    if (preferences.SmsEnabled && !string.IsNullOrEmpty(user.PhoneNumber))
                    {
                        var smsContent = FormatBulkAlertSmsContent(smsTemplate, alerts);
                        tasks.Add(_smsService.SendSmsAsync(user.PhoneNumber, smsContent));
                    }
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Toplu uyarı bildirimi tamamlandı: {AlertCount} uyarı, {SuccessCount}/{TotalCount} başarılı",
                    alerts.Count, results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu uyarı bildirimi gönderilirken hata oluştu: {AlertCount} uyarı", alerts?.Count);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendModelPerformanceNotificationAsync(string modelName, double metricChange)
        {
            try
            {
                _logger.LogInformation("Model performans bildirimi gönderiliyor: {ModelName}, Değişim: {MetricChange:P2}", 
                    modelName, metricChange);

                // 1. Model performans şablonlarını hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("model_performance_email");

                // 2. Alıcıları belirle (genellikle veri bilimciler ve yöneticiler)
                var recipients = await DetermineModelPerformanceRecipientsAsync();
                var tasks = new List<Task<bool>>();
                
                var isNegativeChange = metricChange < 0;
                var subject = isNegativeChange
                    ? $"⚠️ Model Performans Düşüşü: {modelName}"
                    : $"📈 Model Performans İyileşmesi: {modelName}";
                
                foreach (var userId in recipients)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null) continue;

                    var preferences = await GetUserNotificationPreferencesAsync(userId);

                    // Email bildirimi
                    if (preferences.EmailEnabled && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailContent = FormatModelPerformanceEmailContent(emailTemplate, modelName, metricChange, user.Name);
                        tasks.Add(_emailService.SendEmailAsync(
                            user.Email,
                            subject,
                            emailContent));
                    }
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Model performans bildirimi tamamlandı: {ModelName}, {SuccessCount}/{TotalCount} başarılı",
                    modelName, results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Model performans bildirimi gönderilirken hata oluştu: {ModelName}", modelName);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> SendDailySummaryAsync(AlertSummary summary)
        {
            try
            {
                _logger.LogInformation("Günlük özet bildirimi gönderiliyor");

                // 1. Günlük özet şablonunu hazırla
                var emailTemplate = await _templateService.GetTemplateAsync("daily_summary_email");

                // 2. Günlük özet almak isteyen kullanıcıları belirle
                var recipients = await DetermineDailySummaryRecipientsAsync();
                var tasks = new List<Task<bool>>();
                
                foreach (var userId in recipients)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null) continue;

                    var preferences = await GetUserNotificationPreferencesAsync(userId);

                    // Günlük özet tercihi varsa email gönder
                    if (preferences.DailySummaryEnabled && !string.IsNullOrEmpty(user.Email))
                    {
                        var emailContent = FormatDailySummaryEmailContent(emailTemplate, summary, user.Name);
                        tasks.Add(_emailService.SendEmailAsync(
                            user.Email,
                            $"Dolandırıcılık Tespit Sistemi - Günlük Özet ({DateTime.Now:dd.MM.yyyy})",
                            emailContent));
                    }
                }

                // Tüm bildirim görevlerinin tamamlanmasını bekle
                var results = await Task.WhenAll(tasks);
                
                _logger.LogInformation("Günlük özet bildirimi tamamlandı: {SuccessCount}/{TotalCount} başarılı",
                    results.Count(r => r), results.Length);
                
                return results.Any() && results.All(r => r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Günlük özet bildirimi gönderilirken hata oluştu");
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<List<NotificationChannel>> GetNotificationChannelsAsync()
        {
            try
            {
                return await _channelRepository.GetActiveChannelsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildirim kanalları alınırken hata oluştu");
                return new List<NotificationChannel>();
            }
        }

        /// <inheritdoc />
        public async Task<UserNotificationPreferences> GetUserNotificationPreferencesAsync(string userId)
        {
            try
            {
                var preferences = await _channelRepository.GetUserPreferencesAsync(userId);
                
                // Eğer kullanıcı tercihleri yoksa varsayılan değerleri kullan
                if (preferences == null)
                {
                    preferences = new UserNotificationPreferences
                    {
                        UserId = userId,
                        EmailEnabled = true,
                        SmsEnabled = false,
                        PushEnabled = true,
                        InAppEnabled = true,
                        MinimumAlertSeverity = AlertSeverity.Medium,
                        DailySummaryEnabled = false,
                        SubscriptionList = new Dictionary<string, bool>
                        {
                            ["FraudAlerts"] = true,
                            ["SystemNotifications"] = true,
                            ["ModelPerformance"] = false
                        }
                    };
                    
                    // Varsayılan tercihleri kaydet
                    await _channelRepository.SaveUserPreferencesAsync(preferences);
                }
                
                return preferences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bildirim tercihleri alınırken hata oluştu: {UserId}", userId);
                
                // Hata durumunda varsayılan değerleri döndür
                return new UserNotificationPreferences
                {
                    UserId = userId,
                    EmailEnabled = true,
                    SmsEnabled = false,
                    PushEnabled = false,
                    InAppEnabled = true,
                    MinimumAlertSeverity = AlertSeverity.High,
                    DailySummaryEnabled = false,
                    SubscriptionList = new Dictionary<string, bool>
                    {
                        ["FraudAlerts"] = true,
                        ["SystemNotifications"] = false,
                        ["ModelPerformance"] = false
                    }
                };
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateUserNotificationPreferencesAsync(string userId, UserNotificationPreferences preferences)
        {
            try
            {
                _logger.LogInformation("Kullanıcı bildirim tercihleri güncelleniyor: {UserId}", userId);
                
                // Kullanıcı ID'sini güncelle (eşleşmiyorsa)
                preferences.UserId = userId;
                
                return await _channelRepository.SaveUserPreferencesAsync(preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı bildirim tercihleri güncellenirken hata oluştu: {UserId}", userId);
                return false;
            }
        }

        #region Helper Methods

        /// <summary>
        /// İlgili uyarı için bildirim alıcılarını belirler
        /// </summary>
        private async Task<List<string>> DetermineRecipientsAsync(FraudAlert alert)
        {
            // Alıcıları belirle:
            // 1. Uyarı atanmışsa, atanan kişi
            // 2. Uyarı kategorisine göre ilgili ekip üyeleri
            // 3. Uyarı önceliğine göre yöneticiler
            
            var recipients = new HashSet<string>();
            
            // Atanan kişi
            if (!string.IsNullOrEmpty(alert.AssignedTo))
            {
                recipients.Add(alert.AssignedTo);
            }
            
            // Uyarı kategorisine göre ekip üyeleri
            var teamMembers = await _channelRepository.GetTeamMembersByAlertTypeAsync(alert.AlertType.ToString());
            foreach (var member in teamMembers)
            {
                recipients.Add(member);
            }
            
            // Yüksek öncelikli uyarılar için yöneticileri ekle
            if (alert.Severity >= AlertSeverity.High)
            {
                var managers = await _channelRepository.GetUsersByRoleAsync("FraudManager");
                foreach (var manager in managers)
                {
                    recipients.Add(manager);
                }
            }
            
            return recipients.ToList();
        }
        
        /// <summary>
        /// Yüksek öncelikli uyarılar için özel alıcıları belirler
        /// </summary>
        private async Task<List<string>> DetermineHighPriorityRecipientsAsync()
        {
            var recipients = new HashSet<string>();
            
            // Güvenlik ekibi
            var securityTeam = await _channelRepository.GetUsersByRoleAsync("SecurityTeam");
            foreach (var member in securityTeam)
            {
                recipients.Add(member);
            }
            
            // Dolandırıcılık yöneticileri
            var fraudManagers = await _channelRepository.GetUsersByRoleAsync("FraudManager");
            foreach (var manager in fraudManagers)
            {
                recipients.Add(manager);
            }
            
            // Üst düzey yöneticiler
            if (_settings.NotifySeniorManagementOnCritical)
            {
                var seniorManagement = await _channelRepository.GetUsersByRoleAsync("SeniorManagement");
                foreach (var manager in seniorManagement)
                {
                    recipients.Add(manager);
                }
            }
            
            return recipients.ToList();
        }
        
        /// <summary>
        /// Çözülen uyarı için bildirim alıcılarını belirler
        /// </summary>
        private async Task<List<string>> DetermineResolutionRecipientsAsync(FraudAlert alert)
        {
            var recipients = new HashSet<string>();
            
            // Uyarı sahibi (oluşturan kişi)
            if (!string.IsNullOrEmpty(alert.CreatedBy))
            {
                recipients.Add(alert.CreatedBy);
            }
            
            // Atanan kişi
            if (!string.IsNullOrEmpty(alert.AssignedTo))
            {
                recipients.Add(alert.AssignedTo);
            }
            
            // Ekip liderleri
            var teamLeaders = await _channelRepository.GetUsersByRoleAsync("TeamLead");
            foreach (var leader in teamLeaders)
            {
                recipients.Add(leader);
            }
            
            return recipients.ToList();
        }
        
        /// <summary>
        /// Toplu uyarı bildirimleri için alıcıları belirler
        /// </summary>
        private async Task<List<string>> DetermineBulkAlertRecipientsAsync()
        {
            var recipients = new HashSet<string>();
            
            // Dolandırıcılık analistleri
            var analysts = await _channelRepository.GetUsersByRoleAsync("FraudAnalyst");
            foreach (var analyst in analysts)
            {
                recipients.Add(analyst);
            }
            
            // Takım liderleri
            var teamLeads = await _channelRepository.GetUsersByRoleAsync("TeamLead");
            foreach (var lead in teamLeads)
            {
                recipients.Add(lead);
            }
            
            return recipients.ToList();
        }
        
        /// <summary>
        /// Model performans bildirimleri için alıcıları belirler
        /// </summary>
        private async Task<List<string>> DetermineModelPerformanceRecipientsAsync()
        {
            var recipients = new HashSet<string>();
            
            // Veri bilimciler
            var dataScientists = await _channelRepository.GetUsersByRoleAsync("DataScientist");
            foreach (var ds in dataScientists)
            {
                recipients.Add(ds);
            }
            
            // ML mühendisleri
            var mlEngineers = await _channelRepository.GetUsersByRoleAsync("MLEngineer");
            foreach (var eng in mlEngineers)
            {
                recipients.Add(eng);
            }
            
            // IT yöneticileri
            var itManagers = await _channelRepository.GetUsersByRoleAsync("ITManager");
            foreach (var manager in itManagers)
            {
                recipients.Add(manager);
            }
            
            return recipients.ToList();
        }
        
        /// <summary>
        /// Günlük özet bildirimi için alıcıları belirler
        /// </summary>
        private async Task<List<string>> DetermineDailySummaryRecipientsAsync()
        {
            // Günlük özet alma tercihi olan kullanıcıları bul
            return await _channelRepository.GetUsersByPreferenceAsync("DailySummaryEnabled", true);
        }
        
        /// <summary>
        /// Email içeriğini formatlar
        /// </summary>
        private string FormatEmailContent(string template, FraudAlert alert, string userName)
        {
            var content = template
                .Replace("{UserName}", userName)
                .Replace("{AlertId}", alert.Id.ToString())
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{AlertDescription}", alert.Description)
                .Replace("{AlertSeverity}", alert.Severity.ToString())
                .Replace("{AlertCreatedAt}", alert.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"))
                .Replace("{TransactionId}", alert.TransactionId.ToString());
                
            return content;
        }
        
        /// <summary>
        /// SMS içeriğini formatlar
        /// </summary>
        private string FormatSmsContent(string template, FraudAlert alert)
        {
            var content = template
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{AlertSeverity}", alert.Severity.ToString())
                .Replace("{AlertId}", alert.Id.ToString());
                
            return content;
        }
        
        /// <summary>
        /// Push bildirim içeriğini formatlar
        /// </summary>
        private string FormatPushContent(string template, FraudAlert alert)
        {
            var content = template
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{AlertSeverity}", alert.Severity.ToString());
                
            return content;
        }
        
        /// <summary>
        /// Atama email içeriğini formatlar
        /// </summary>
        private string FormatAssignmentEmailContent(string template, FraudAlert alert, string userName)
        {
            var content = template
                .Replace("{UserName}", userName)
                .Replace("{AlertId}", alert.Id.ToString())
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{AlertDescription}", alert.Description)
                .Replace("{AlertSeverity}", alert.Severity.ToString())
                .Replace("{AlertCreatedAt}", alert.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"))
                .Replace("{TransactionId}", alert.TransactionId.ToString());
                
            return content;
        }
        
        /// <summary>
        /// Atama push bildirim içeriğini formatlar
        /// </summary>
        private string FormatAssignmentPushContent(string template, FraudAlert alert)
        {
            var content = template
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{AlertSeverity}", alert.Severity.ToString());
                
            return content;
        }
        
        /// <summary>
        /// Çözüm email içeriğini formatlar
        /// </summary>
        private string FormatResolutionEmailContent(string template, FraudAlert alert, string userName, string resolvedBy)
        {
            var user = _userRepository.GetByIdAsync(resolvedBy).Result;
            var resolverName = user?.Name ?? resolvedBy;
            
            var content = template
                .Replace("{UserName}", userName)
                .Replace("{ResolverName}", resolverName)
                .Replace("{AlertId}", alert.Id.ToString())
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{AlertDescription}", alert.Description)
                .Replace("{Resolution}", alert.Resolution)
                .Replace("{ResolvedAt}", alert.ResolvedAt?.ToString("dd.MM.yyyy HH:mm:ss") ?? "");
                
            return content;
        }
        
        /// <summary>
        /// Çözüm push bildirim içeriğini formatlar
        /// </summary>
        private string FormatResolutionPushContent(string template, FraudAlert alert, string resolvedBy)
        {
            var user = _userRepository.GetByIdAsync(resolvedBy).Result;
            var resolverName = user?.Name ?? resolvedBy;
            
            var content = template
                .Replace("{AlertTitle}", alert.Title)
                .Replace("{ResolverName}", resolverName);
                
            return content;
        }
        
        /// <summary>
        /// Toplu uyarı email içeriğini formatlar
        /// </summary>
        private string FormatBulkAlertEmailContent(string template, List<FraudAlert> alerts, string userName)
        {
            var alertsHtml = new StringBuilder();
            
            foreach (var alert in alerts)
            {
                alertsHtml.AppendLine("<tr>");
                alertsHtml.AppendLine($"<td>{alert.Id}</td>");
                alertsHtml.AppendLine($"<td>{alert.Title}</td>");
                alertsHtml.AppendLine($"<td>{alert.Severity}</td>");
                alertsHtml.AppendLine($"<td>{alert.CreatedAt:dd.MM.yyyy HH:mm}</td>");
                alertsHtml.AppendLine("</tr>");
            }
            
            var highPriorityCount = alerts.Count(a => a.Severity >= AlertSeverity.High);
            
            var content = template
                .Replace("{UserName}", userName)
                .Replace("{AlertCount}", alerts.Count.ToString())
                .Replace("{HighPriorityCount}", highPriorityCount.ToString())
                .Replace("{AlertsTable}", alertsHtml.ToString())
                .Replace("{Date}", DateTime.Now.ToString("dd.MM.yyyy"));
                
            return content;
        }
        
        /// <summary>
        /// Toplu uyarı SMS içeriğini formatlar
        /// </summary>
        private string FormatBulkAlertSmsContent(string template, List<FraudAlert> alerts)
        {
            var highPriorityCount = alerts.Count(a => a.Severity >= AlertSeverity.High);
            
            var content = template
                .Replace("{AlertCount}", alerts.Count.ToString())
                .Replace("{HighPriorityCount}", highPriorityCount.ToString())
                .Replace("{Date}", DateTime.Now.ToString("dd.MM.yyyy"));
                
            return content;
        }
        
        /// <summary>
        /// Model performans email içeriğini formatlar
        /// </summary>
        private string FormatModelPerformanceEmailContent(string template, string modelName, double metricChange, string userName)
        {
            var changeDirection = metricChange >= 0 ? "artış" : "düşüş";
            var changeClass = metricChange >= 0 ? "positive" : "negative";
            
            var content = template
                .Replace("{UserName}", userName)
                .Replace("{ModelName}", modelName)
                .Replace("{MetricChange}", Math.Abs(metricChange).ToString("P2"))
                .Replace("{ChangeDirection}", changeDirection)
                .Replace("{ChangeClass}", changeClass)
                .Replace("{Date}", DateTime.Now.ToString("dd.MM.yyyy"));
                
            return content;
        }
        
        /// <summary>
        /// Günlük özet email içeriğini formatlar
        /// </summary>
        private string FormatDailySummaryEmailContent(string template, AlertSummary summary, string userName)
        {
            var content = template
                .Replace("{UserName}", userName)
                .Replace("{TotalAlerts}", summary.TotalAlerts.ToString())
                .Replace("{ActiveAlerts}", summary.ActiveAlerts.ToString())
                .Replace("{ResolvedAlerts}", summary.ResolvedAlerts.ToString())
                .Replace("{HighSeverity}", summary.HighSeverity.ToString())
                .Replace("{MediumSeverity}", summary.MediumSeverity.ToString())
                .Replace("{LowSeverity}", summary.LowSeverity.ToString())
                .Replace("{PendingReview}", summary.PendingReview.ToString())
                .Replace("{Date}", DateTime.Now.ToString("dd.MM.yyyy"));
                
            return content;
        }

        /// <summary>
        /// Acil durum webhook bildirimi gönderir
        /// </summary>
        private async Task<bool> SendEmergencyWebhookAsync(FraudAlert alert)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    var payload = new
                    {
                        alert.Id,
                        alert.Title,
                        alert.Description,
                        Severity = alert.Severity.ToString(),
                        alert.CreatedAt,
                        alert.TransactionId,
                        Type = "EMERGENCY_ALERT"
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(payload);
                    var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync(_settings.EmergencyWebhookUrl, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Acil durum webhook bildirimi gönderilirken hata oluştu: {AlertId}", alert.Id);
                return false;
            }
        }
        
        #endregion
    }

    /// <summary>
    /// Bildirim ayarları
    /// </summary>
    public class NotificationSettings
    {
        /// <summary>
        /// Email servis URL'si
        /// </summary>
        public string EmailServiceUrl { get; set; }
        
        /// <summary>
        /// SMS servis URL'si
        /// </summary>
        public string SmsServiceUrl { get; set; }
        
        /// <summary>
        /// Push notification servis URL'si
        /// </summary>
        public string PushServiceUrl { get; set; }
        
        /// <summary>
        /// Webhook URL'si
        /// </summary>
        public string WebhookUrl { get; set; }
        
        /// <summary>
        /// Acil durum webhook URL'si
        /// </summary>
        public string EmergencyWebhookUrl { get; set; }
        
        /// <summary>
        /// Maksimum deneme sayısı
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        
        /// <summary>
        /// Yeniden deneme aralığı (saniye)
        /// </summary>
        public int RetryInterval { get; set; } = 30;
        
        /// <summary>
        /// Mesajlar için önbellek süresi (dakika)
        /// </summary>
        public int MessageCacheDuration { get; set; } = 60;
        
        /// <summary>
        /// Üst düzey yöneticilere kritik uyarı bildirimlerini gönder
        /// </summary>
        public bool NotifySeniorManagementOnCritical { get; set; } = true;
    }
}