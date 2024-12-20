using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class Feature
{
    public string Name { get; init; }
    public FeatureType Type { get; init; }
    public IDataView Value { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}