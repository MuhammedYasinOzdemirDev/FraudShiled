namespace FraudShield.TransactionAnalysis.ML.Models.PCA.Analysis;

public class PCAAnalysisResult
{
    public double[] PrincipalComponents { get; set; }
    public double ExplainedVarianceRatio { get; set; }
    public double ReconstructionError { get; set; }
    public DateTime AnalyzedAt { get; set; }
}
