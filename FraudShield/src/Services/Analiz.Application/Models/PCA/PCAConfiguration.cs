namespace Analiz.ML.Models.PCA;

public class PCAConfiguration
{
    public int ComponentCount { get; set; } = 3;
    public double ExplainedVarianceThreshold { get; set; } = 0.95;
    public bool StandardizeInput { get; set; } = true;
    public double AnomalyThreshold { get; set; } = 2.0;
    public List<string> FeatureColumns { get; set; }
}
