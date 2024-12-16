namespace FraudShield.TransactionAnalysis.Domain.Models;

public record ModelMetrics
{
    public double Accuracy { get; init; }
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
    public double AUC { get; init; }
    public Dictionary<string, double> CustomMetrics { get; init; }
    public DateTime CalculatedAt { get; init; }
}
