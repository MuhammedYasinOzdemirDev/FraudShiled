using Microsoft.ML;

namespace FraudShield.TransactionAnalysis.Domain.Models;

 public class EvaluationData
    {
        public IDataView Data { get; }
        public Dictionary<string, string> Metadata { get; }

        public EvaluationData(IDataView data, Dictionary<string, string> metadata = null)
        {
            Data = data;
            Metadata = metadata ?? new Dictionary<string, string>();
        }
    }