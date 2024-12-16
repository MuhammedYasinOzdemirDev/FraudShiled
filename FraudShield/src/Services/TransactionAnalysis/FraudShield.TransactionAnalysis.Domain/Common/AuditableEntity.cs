using FraudShield.TransactionAnalysis.Domain.Models;

namespace FraudShield.TransactionAnalysis.Domain.Common;

public abstract class AuditableEntity : Entity
{
    private readonly List<AuditLog> _auditLogs = new();
    public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs.AsReadOnly();

    protected void AddAuditLog(string action, string details, string userId)
    {
        _auditLogs.Add(new AuditLog
        {
            Action = action,
            Details = details,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });
    }
}