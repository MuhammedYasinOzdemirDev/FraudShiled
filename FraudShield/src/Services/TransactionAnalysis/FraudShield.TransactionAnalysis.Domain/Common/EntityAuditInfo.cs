namespace FraudShield.TransactionAnalysis.Domain.Common;

public class EntityAuditInfo
{
    public Guid EntityId { get; private set; }
    public string EntityType { get; private set; }
    public string Action { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string UserId { get; private set; }
    public string Changes { get; private set; }

    private EntityAuditInfo() { }

    public static EntityAuditInfo Create(
        Guid entityId,
        string entityType,
        string action,
        string userId,
        string changes)
    {
        return new EntityAuditInfo
        {
            EntityId = entityId,
            EntityType = entityType,
            Action = action,
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            Changes = changes
        };
    }
}
