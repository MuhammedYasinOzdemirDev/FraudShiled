namespace Analiz.Domain.Entities.ML;

public class ModelMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AUC { get; set; }
    public Dictionary<string, double> AdditionalMetrics { get; set; }

    public ModelMetrics()
    {
        AdditionalMetrics = new Dictionary<string, double>();
    }

    public Dictionary<string, double> ToDictionary()
    {
        var metrics = new Dictionary<string, double>
        {
            ["Accuracy"] = Accuracy,
            ["Precision"] = Precision,
            ["Recall"] = Recall,
            ["F1Score"] = F1Score,
            ["AUC"] = AUC
        };

        foreach (var metric in AdditionalMetrics)
        {
            metrics[metric.Key] = metric.Value;
        }

        return metrics;
    }
}