using System.Text.Json;

namespace FraudShield.TransactionAnalysis.Domain.ValueObjects;
public record AnalysisMetrics
{
    public decimal AnomalyScore { get; init; }
    public decimal PatternMatchScore { get; init; }
    public decimal BehaviorScore { get; init; }
    public decimal VelocityScore { get; init; }
    public IDictionary<string, decimal> FeatureImportance { get; init; }
    public IReadOnlyList<string> DetectedPatterns { get; init; }
    public DateTime CalculatedAt { get; init; }
    public string AdditionalMetricsJson { get; init; }

    private AnalysisMetrics()
    {
        FeatureImportance = new Dictionary<string, decimal>();
        DetectedPatterns = new List<string>();
    }

    public static AnalysisMetrics Initial() => new AnalysisMetrics
    {
        AnomalyScore = 0,
        PatternMatchScore = 0,
        BehaviorScore = 0,
        VelocityScore = 0,
        FeatureImportance = new Dictionary<string, decimal>(),
        DetectedPatterns = new List<string>(),
        CalculatedAt = DateTime.UtcNow,
        AdditionalMetricsJson = "{}"
    };

    public static AnalysisMetrics Create(
        decimal anomalyScore,
        decimal patternMatchScore,
        decimal behaviorScore,
        decimal velocityScore,
        IDictionary<string, decimal> featureImportance,
        IEnumerable<string> detectedPatterns,
        IDictionary<string, object> additionalMetrics = null)
    {
        return new AnalysisMetrics
        {
            AnomalyScore = anomalyScore,
            PatternMatchScore = patternMatchScore,
            BehaviorScore = behaviorScore,
            VelocityScore = velocityScore,
            FeatureImportance = featureImportance ?? new Dictionary<string, decimal>(),
            DetectedPatterns = detectedPatterns?.ToList() ?? new List<string>(),
            CalculatedAt = DateTime.UtcNow,
            AdditionalMetricsJson = additionalMetrics != null ? 
                JsonSerializer.Serialize(additionalMetrics) : "{}"
        };
    }

    public IDictionary<string, object> GetAdditionalMetrics()
    {
        if (string.IsNullOrEmpty(AdditionalMetricsJson))
            return new Dictionary<string, object>();

        return JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalMetricsJson);
    }

    // Kolaylık sağlayacak extension metodu
    
}
public static class AnalysisMetricsExtensions
{
    public static decimal CalculateOverallScore(this AnalysisMetrics metrics)
    {
        return (metrics.AnomalyScore + 
                metrics.PatternMatchScore + 
                metrics.BehaviorScore + 
                metrics.VelocityScore) / 4;
    }

    public static bool HasHighRiskIndicators(AnalysisMetrics metrics)
    {
        return metrics.AnomalyScore >= 0.8m ||
               metrics.PatternMatchScore >= 0.8m ||
               metrics.BehaviorScore >= 0.8m ||
               metrics.VelocityScore >= 0.8m;
    }

    public static IEnumerable<string> GetHighRiskFactors(this AnalysisMetrics metrics)
    {
        var factors = new List<string>();

        if (metrics.AnomalyScore >= 0.8m)
            factors.Add("High Anomaly Score");
        if (metrics.PatternMatchScore >= 0.8m)
            factors.Add("High Pattern Match");
        if (metrics.BehaviorScore >= 0.8m)
            factors.Add("High Behavior Risk");
        if (metrics.VelocityScore >= 0.8m)
            factors.Add("High Velocity Risk");

        return factors;
    }
}