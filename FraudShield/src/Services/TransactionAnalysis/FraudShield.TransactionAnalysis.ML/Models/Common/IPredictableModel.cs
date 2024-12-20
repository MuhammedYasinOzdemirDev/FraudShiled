using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Models;

namespace FraudShield.TransactionAnalysis.ML.Models.Common;

public interface IPredictableModel
{
    Task<Result<PredictionResult>> PredictAsync(PredictionData data);
    Task<Result<IEnumerable<PredictionResult>>> PredictBatchAsync(IEnumerable<PredictionData> data);
}

