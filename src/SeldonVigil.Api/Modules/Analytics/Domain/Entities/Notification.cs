using SeldonVigil.Api.Shared.Domain;

namespace SeldonVigil.Api.Modules.Analytics.Domain.Entities;

internal sealed class Notification : BaseEntity
{
    public string Type { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? AgentId { get; private set; }
    public string Severity { get; private set; } = "info";
    public bool Read { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? TenantId { get; private set; }

    private Notification() { }

    public static Notification Create(
        string type,
        string title,
        string message,
        string? agentId = null,
        string severity = "info",
        string? tenantId = null)
    {
        return new Notification
        {
            Type = type,
            Title = title,
            Message = message,
            AgentId = agentId,
            Severity = severity,
            TenantId = tenantId
        };
    }

    public void MarkAsRead()
    {
        Read = true;
        ReadAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
