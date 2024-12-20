using FraudShield.TransactionAnalysis.Domain.Enums;

namespace FraudShield.TransactionAnalysis.Domain.Models;

public class RawData
{
    public IEnumerable<TransactionRecord> Records { get; init; }
    public DataSource Source { get; init; }
    public DateTime CollectedAt { get; init; }
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}