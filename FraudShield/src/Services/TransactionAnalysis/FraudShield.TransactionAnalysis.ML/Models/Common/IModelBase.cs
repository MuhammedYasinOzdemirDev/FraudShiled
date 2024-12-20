using FraudShield.TransactionAnalysis.ML.Models.Common.Enums;

namespace FraudShield.TransactionAnalysis.ML.Models.Common;

public interface IModelBase
{
    Guid Id { get; }
    string Name { get; }
    string Version { get; }
    ModelType Type { get; }
    DateTime CreatedAt { get; }
    ModelStatus Status { get; }
    IDictionary<string, double> Metrics { get; }
}