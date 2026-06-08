namespace CivicLedger.Domain;

public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    public AuditEntry(Guid grantId, string action, string details)
    {
        Id = Guid.NewGuid();
        GrantId = grantId;
        Action = action;
        Details = details;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid GrantId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
}
