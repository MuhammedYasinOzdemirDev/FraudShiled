namespace FraudShield.TransactionAnalysis.Domain.Models;

public record AnalysisFeatures
{
    public IDictionary<string, double> NumericalFeatures { get; init; }
    public IDictionary<string, string> CategoricalFeatures { get; init; }
    public IDictionary<string, bool> BooleanFeatures { get; init; }
    
    public static AnalysisFeatures Create() => new()
    {
        NumericalFeatures = new Dictionary<string, double>(),
        CategoricalFeatures = new Dictionary<string, string>(),
        BooleanFeatures = new Dictionary<string, bool>()
    };
}