using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities.ML;

public class TrainingRequest
{
    public string ModelName { get; set; }
    public ModelType ModelType { get; set; }
    public List<TransactionData> TrainingData { get; set; }
    public List<bool> Labels { get; set; }
    public List<TransactionData> ValidationData { get; set; }
    public string Configuration { get; set; }
}