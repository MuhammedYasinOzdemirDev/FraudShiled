namespace Analiz.ML.Models.LightGBM;

public class LightGBMConfiguration
{
    // Temel Model Parametreleri
    public int NumberOfLeaves { get; set; } = 31;
    public int MinDataInLeaf { get; set; } = 20;
    public double LearningRate { get; set; } = 0.05;
    public int NumberOfTrees { get; set; } = 100;
    
    // Feature Parametreleri
    public double FeatureFraction { get; set; } = 0.9;
    public double BaggingFraction { get; set; } = 0.8;
    public int BaggingFrequency { get; set; } = 5;
    public List<string> FeatureColumns { get; set; }
    
    // Regularizasyon Parametreleri
    public double L1Regularization { get; set; } = 0.0;
    public double L2Regularization { get; set; } = 0.0;
    
    // Early Stopping Parametreleri
    public int EarlyStoppingRound { get; set; } = 50;
    public double MinGainToSplit { get; set; } = 0.0;
    
    // Sınıf Dengesizliği için Parametreler
    public bool UseClassWeights { get; set; } = true;
    public Dictionary<string, double> ClassWeights { get; set; }
    
    // Threshold Parametreleri
    public double PredictionThreshold { get; set; } = 0.5;
    public bool UseDynamicThreshold { get; set; } = false;
    
    public LightGBMConfiguration()
    {
        FeatureColumns = new List<string>();
        ClassWeights = new Dictionary<string, double>
        {
            {"0", 1.0},  // Normal işlemler
            {"1", 5.0}   // Dolandırıcılık işlemleri (daha yüksek ağırlık)
        };
    }
}
