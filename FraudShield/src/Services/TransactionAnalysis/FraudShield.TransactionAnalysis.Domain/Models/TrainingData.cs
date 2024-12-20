using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class TrainingData
{
    public IDataView Data { get; init; }
    public EvaluationData ValidationData { get; init; }
    public DateTime DataFrom { get; init; }
    public DateTime DataTo { get; init; }
    public IDictionary<string, string> Metadata { get; init; }
}
