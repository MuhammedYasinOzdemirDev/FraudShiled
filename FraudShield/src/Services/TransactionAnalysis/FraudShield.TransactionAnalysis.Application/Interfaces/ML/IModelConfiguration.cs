namespace FraudShield.TransactionAnalysis.Application.Interfaces.ML;

public interface IModelConfiguration
{
    string ModelName { get; }
    string Version { get; }
    IDictionary<string, object> HyperParameters { get; }
    IEnumerable<string> FeatureColumns { get; }
}