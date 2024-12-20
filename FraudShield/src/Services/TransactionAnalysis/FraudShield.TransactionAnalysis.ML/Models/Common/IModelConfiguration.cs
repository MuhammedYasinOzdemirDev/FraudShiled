using FraudShield.TransactionAnalysis.ML.Models.Common.Enums;

namespace FraudShield.TransactionAnalysis.ML.Models.Common;


public interface IModelConfiguration
{
    string ModelName { get; }
    string Version { get; }
    IDictionary<string, object> HyperParameters { get; }
    IEnumerable<string> FeatureColumns { get; }
    ModelType Type { get; }
}
