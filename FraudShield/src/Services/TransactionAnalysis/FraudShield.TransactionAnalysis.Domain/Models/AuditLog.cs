namespace FraudShield.TransactionAnalysis.Domain.Models;

public class AuditLog
{
    public string Action { get; set; }
    public string Details { get; set; }
    public string UserId { get; set; }
    public DateTime Timestamp { get; set; }
}