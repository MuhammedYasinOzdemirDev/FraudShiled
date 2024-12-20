using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Application.Interfaces.ML;
public interface IModel
{
    Guid Id { get; }
    string Name { get; }
    string Version { get; }
    ModelType Type { get; }
    IDictionary<string, double> Metrics { get; }
}