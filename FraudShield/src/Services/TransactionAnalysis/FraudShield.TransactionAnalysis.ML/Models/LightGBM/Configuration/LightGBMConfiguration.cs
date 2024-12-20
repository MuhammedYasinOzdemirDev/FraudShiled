using FraudShield.TransactionAnalysis.Application.Interfaces.ML;
using FraudShield.TransactionAnalysis.ML.Models.Common.Enums;

namespace FraudShield.TransactionAnalysis.ML.Models.LightGBM.Configuration;

public class LightGBMConfiguration : IModelConfiguration
{
    public string ModelName { get; set; }
    public string Version { get; set; }
    public ModelType Type => ModelType.LightGBM;
    
    public IDictionary<string, object> HyperParameters { get; set; } = new Dictionary<string, object>
    {
        ["num_leaves"] = 31,
        ["learning_rate"] = 0.1,
        ["feature_fraction"] = 0.8,
        ["bagging_fraction"] = 0.8,
        ["bagging_freq"] = 5,
        ["min_data_in_leaf"] = 20,
        ["max_depth"] = 15,
        ["num_iterations"] = 100
    };

    public IEnumerable<string> FeatureColumns { get; set; }

    public static LightGBMConfiguration Default(string modelName, string version)
    {
        return new LightGBMConfiguration
        {
            ModelName = modelName,
            Version = version,
            FeatureColumns = Array.Empty<string>()
        };
    }
}