namespace Analiz.Domain.Entities;

public class AlertSummary
{
    public int TotalAlerts { get; set; }
    public int ActiveAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int HighSeverity { get; set; }
    public int MediumSeverity { get; set; }
    public int LowSeverity { get; set; }
    public int PendingReview { get; set; }
    public Dictionary<string, int> AlertsByCategory { get; set; }
}