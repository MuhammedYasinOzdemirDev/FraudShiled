namespace Analiz.Domain.Entities.ML;

public class ModelInput
{
    public float[] Features { get; set; }
    public bool Label { get; set; }
}