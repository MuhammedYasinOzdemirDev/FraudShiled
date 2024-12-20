namespace FraudShield.TransactionAnalysis.Domain.Models;

public class ModelOutput
{
    public bool PredictedLabel { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
    public float[] FeatureContributions { get; set; }
}