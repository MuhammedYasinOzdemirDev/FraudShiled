namespace FraudShield.TransactionAnalysis.Domain.Models;

public class ModelMetrics
{
    public double Accuracy { get; init; }
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
    public double AUC { get; init; }
    public ConfusionMatrix ConfusionMatrix { get; init; }
    public DateTime CalculatedAt { get; init; }
    public IDictionary<string, double> CustomMetrics { get; set; }
    public double[] ExplainedVariance { get; init; }
    public double CumulativeExplainedVariance { get; init; }
    public double ReconstructionError { get; init; }
    public int ComponentCount { get; init; }

    public ModelMetrics()
    {
        CalculatedAt = DateTime.UtcNow;
        ConfusionMatrix = new ConfusionMatrix();
        CustomMetrics = new Dictionary<string, double>();
    }

    public IDictionary<string, double> ToDictionary()
    {
        var metrics = new Dictionary<string, double>
        {
            ["Accuracy"] = Accuracy,
            ["Precision"] = Precision,
            ["Recall"] = Recall,
            ["F1Score"] = F1Score,
            ["AUC"] = AUC,
            ["ConfusionMatrix_TruePositives"] = ConfusionMatrix.TruePositives,
            ["ConfusionMatrix_TrueNegatives"] = ConfusionMatrix.TrueNegatives,
            ["ConfusionMatrix_FalsePositives"] = ConfusionMatrix.FalsePositives,
            ["ConfusionMatrix_FalseNegatives"] = ConfusionMatrix.FalseNegatives
        };

        foreach (var customMetric in CustomMetrics)
        {
            metrics[customMetric.Key] = customMetric.Value;
        }

        return metrics;
    }
}