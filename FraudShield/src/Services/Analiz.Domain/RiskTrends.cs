namespace Analiz.Domain;

public class RiskTrends
{
    public string UserId { get; set; }
    public TimeSpan Period { get; set; }
    public Dictionary<DateTime, double> DailyScores { get; set; }
    public Dictionary<string, int> RiskFactorFrequency { get; set; }
    public TrendLineData TrendLine { get; set; }
    public DateTime LastUpdated { get; set; }
}
