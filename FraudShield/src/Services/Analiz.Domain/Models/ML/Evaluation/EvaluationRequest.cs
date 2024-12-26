namespace Analiz.Domain.Entities.ML.Evaluation;

public class EvaluationRequest
{
    public string ModelName { get; set; }
    public string Version { get; set; }
    public List<TransactionData> EvaluationData { get; set; }
    public List<bool> Labels { get; set; }
}