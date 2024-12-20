using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Models;

namespace FraudShield.TransactionAnalysis.ML.Models.Common;
public interface ITrainableModel
{
    Task<Result<IModelBase>> TrainAsync(TrainingData trainingData, CancellationToken cancellationToken = default);
    Task<Result<ModelMetrics>> EvaluateAsync(EvaluationData evaluationData);
}