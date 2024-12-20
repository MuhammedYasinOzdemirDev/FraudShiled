using FraudShield.TransactionAnalysis.Domain.Models;
using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.ML.Pipeline.DataPreprocessing;

public class ProcessedData
{
    public IDataView Data { get; init; }
    public ProcessingMetrics Metrics { get; init; }
    public DataSchema Schema { get; init; }
    public DateTime ProcessedAt { get; init; }
}