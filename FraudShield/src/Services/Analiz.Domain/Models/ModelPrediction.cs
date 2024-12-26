namespace Analiz.Domain.Entities;

public class ModelPrediction
{
    public bool PredictedLabel { get; set; }
    public double Score { get; set; }
    public double Probability { get; set; }
}
