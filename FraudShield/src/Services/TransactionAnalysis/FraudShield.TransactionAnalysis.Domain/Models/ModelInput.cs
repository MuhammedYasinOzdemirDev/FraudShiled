namespace FraudShield.TransactionAnalysis.Domain.Models;

public class ModelInput
{
    public float Amount { get; set; }
    public int TransactionHour { get; set; }
    public int TransactionDay { get; set; }
    public float MerchantRiskScore { get; set; }
    public bool Label { get; set; }
}
