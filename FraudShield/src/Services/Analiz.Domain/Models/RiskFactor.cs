using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class RiskFactor
{
    public string Code { get; set; }
    public string Description { get; set; }
    public double Weight { get; set; }
    public RiskLevel Severity { get; set; }
}