using FraudShield.TransactionAnalysis.Application.Interfaces.ML;
using FraudShield.TransactionAnalysis.ML.Models.Common.Enums;

namespace FraudShield.TransactionAnalysis.ML.Models.PCA.Configuration;

public class PCAModelConfiguration
{
    public int NumberOfComponents { get; set; } = 3;
    public bool CenterData { get; set; } = true;
    public bool ScaleData { get; set; } = true;
    public double VarianceRetained { get; set; } = 0.95;
    public string[] FeatureColumns { get; set; }
}
