namespace FraudShield.TransactionAnalysis.ML.Pipeline.DataPreprocessing.Options;

public class PreprocessingOptions
{
    public bool HandleMissingValues { get; set; } = true;
    public bool DetectOutliers { get; set; } = true;
    public bool NormalizeFeatures { get; set; } = true;
    public bool FailOnValidationError { get; set; } = false;
    public double OutlierDetectionConfidence { get; set; } = 95.0;
    public int OutlierDetectionHistoryLength { get; set; } = 30;
    public string[] RequiredColumns { get; set; } = new[] { "Amount", "TransactionType", "UserId" };
}