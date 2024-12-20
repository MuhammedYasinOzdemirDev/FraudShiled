using FraudShield.TransactionAnalysis.Domain.Common;
using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.ML.Models.PCA.Analysis;

public interface IPCAAnalyzer
{
    Task<Result<PCAAnalysisResult>> AnalyzeAsync(IDataView data);
    Task<Result<double>> CalculateAnomalyScoreAsync(IDataView data);
}