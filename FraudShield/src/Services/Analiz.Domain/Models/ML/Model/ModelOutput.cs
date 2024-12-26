using Microsoft.ML.Data;

namespace Analiz.Domain.Entities.ML;

public class ModelOutput
{
    public bool PredictedLabel { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}
public class PCAModelOutput : ModelOutput
{
    [VectorType]
    public float[] PCAFeatures { get; set; }
    
    public float AnomalyScore { get; set; }
    
    public bool IsAnomaly { get; set; }
}

public class LightGBMModelOutput
{
    public bool PredictedLabel { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
    
    [VectorType]
    public float[] Features { get; set; }
}