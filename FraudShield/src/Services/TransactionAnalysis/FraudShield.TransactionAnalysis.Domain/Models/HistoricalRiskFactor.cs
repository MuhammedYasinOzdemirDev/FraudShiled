namespace FraudShield.TransactionAnalysis.Domain.Models;

public class HistoricalRiskFactor
{
    public string Factor { get; private set; }
    public decimal Impact { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public HistoricalRiskFactor(string factor, decimal impact, DateTime occurredAt)
    {
        Factor = factor;
        Impact = impact;
        OccurredAt = occurredAt;
    }
}