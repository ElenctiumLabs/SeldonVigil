using SeldonVigil.Api.Shared.Domain;

namespace SeldonVigil.Api.Modules.Integration.Domain.Entities;

internal enum IntegrationType
{
    Slack,
    Teams,
    PagerDuty,
    Jira,
    GitHub,
    Webhook,
    Email
}

internal enum IntegrationStatus
{
    Active,
    Inactive,
    Error,
    PendingConfiguration
}

internal sealed class IntegrationConfiguration : BaseEntity
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public IntegrationType Type { get; private set; }
    public IntegrationStatus Status { get; private set; }
    public string ConfigurationJson { get; private set; } = "{}";
    public string? WebhookUrl { get; private set; }
    public string? ApiKey { get; private set; }
    public string? ChannelId { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public int ErrorCount { get; private set; }

    private IntegrationConfiguration() { }

    public static IntegrationConfiguration Create(
        string tenantId,
        string name,
        IntegrationType type,
        string configurationJson,
        string? webhookUrl = null,
        string? apiKey = null)
    {
        return new IntegrationConfiguration
        {
            TenantId = tenantId,
            Name = name,
            Type = type,
            ConfigurationJson = configurationJson,
            WebhookUrl = webhookUrl,
            ApiKey = apiKey,
            Status = IntegrationStatus.PendingConfiguration
        };
    }

    public void Activate()
    {
        Status = IntegrationStatus.Active;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        Status = IntegrationStatus.Inactive;
        MarkAsUpdated();
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
    }

    public void RecordError()
    {
        ErrorCount++;
        if (ErrorCount >= 5)
        {
            Status = IntegrationStatus.Error;
        }
        MarkAsUpdated();
    }

    public void ClearErrors()
    {
        ErrorCount = 0;
        Status = IntegrationStatus.Active;
        MarkAsUpdated();
    }

    public void Update(string? name = null, string? configurationJson = null, string? webhookUrl = null, string? apiKey = null)
    {
        if (name is not null) Name = name;
        if (configurationJson is not null) ConfigurationJson = configurationJson;
        if (webhookUrl is not null) WebhookUrl = webhookUrl;
        if (apiKey is not null) ApiKey = apiKey;
        MarkAsUpdated();
    }
}
