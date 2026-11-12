using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.AIReasoning.Domain.Entities;
using SeldonVigil.Api.Modules.Analytics.Domain.Entities;
using SeldonVigil.Api.Modules.Domain.Infrastructure.Persistence.Entities;
using SeldonVigil.Api.Modules.Integration.Domain.Entities;
using SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using SeldonVigil.Shared.Domain.Outbox;
using SeldonVigil.Shared.Domain.Saga;

namespace SeldonVigil.Api.Shared.Infrastructure.Persistence;

[method: RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
[method: RequiresDynamicCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
internal sealed class SeldonVigilDbContext(
    DbContextOptions<SeldonVigilDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    // Identity
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentTrustRelationship> AgentTrustRelationships => Set<AgentTrustRelationship>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // Security
    public DbSet<SecurityPolicy> SecurityPolicies => Set<SecurityPolicy>();
    public DbSet<SecurityViolation> SecurityViolations => Set<SecurityViolation>();

    // Observability
    public DbSet<TraceEntity> Traces => Set<TraceEntity>();

    // Cost Intelligence
    public DbSet<CostBudget> CostBudgets => Set<CostBudget>();

    // Governance
    public DbSet<GovernancePolicy> GovernancePolicies => Set<GovernancePolicy>();
    public DbSet<CertificationPipeline> CertificationPipelines => Set<CertificationPipeline>();

    // AI Reasoning
    public DbSet<RootCauseAnalysis> RootCauseAnalyses => Set<RootCauseAnalysis>();

    // Analytics
    public DbSet<Notification> Notifications => Set<Notification>();

    // Integration
    public DbSet<IntegrationConfiguration> Integrations => Set<IntegrationConfiguration>();

    // Domain
    public DbSet<PromptVersionEntity> PromptVersions => Set<PromptVersionEntity>();
    public DbSet<EvaluationResultEntity> EvaluationResults => Set<EvaluationResultEntity>();

    // Saga
    public DbSet<SagaInstance> SagaInstances => Set<SagaInstance>();

    // Outbox
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    [RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func<Type, Boolean>)")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        IgnoreDomainEvents(modelBuilder);

        ConfigureIdentity(modelBuilder);
        ConfigureSecurity(modelBuilder);
        ConfigureObservability(modelBuilder);
        ConfigureCostIntelligence(modelBuilder);
        ConfigureGovernance(modelBuilder);
        ConfigureAIReasoning(modelBuilder);
        ConfigureAnalytics(modelBuilder);
        ConfigureIntegration(modelBuilder);
        ConfigureDomain(modelBuilder);
        ConfigureSaga(modelBuilder);
        ConfigureOutbox(modelBuilder);

        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => entityType.FindNavigation("DomainEvents") is not null))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Entities with TenantId value objects — fail-closed tenant isolation
        modelBuilder.Entity<Agent>().HasQueryFilter(a => a.TenantId.Equals(_tenantContext.CurrentTenant));
        modelBuilder.Entity<AgentTrustRelationship>().HasQueryFilter(t => t.TenantId.Equals(_tenantContext.CurrentTenant));
        modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId.Equals(_tenantContext.CurrentTenant));
        modelBuilder.Entity<UserSession>().HasQueryFilter(s => s.TenantId.Equals(_tenantContext.CurrentTenant));
        modelBuilder.Entity<SecurityPolicy>().HasQueryFilter(p => p.TenantId.Equals(_tenantContext.CurrentTenant));
        modelBuilder.Entity<SecurityViolation>().HasQueryFilter(v => v.TenantId.Equals(_tenantContext.CurrentTenant));
        modelBuilder.Entity<CostBudget>().HasQueryFilter(b => b.TenantId.Equals(_tenantContext.CurrentTenant));

        // TraceEntity uses raw Guid TenantId
        modelBuilder.Entity<TraceEntity>().HasQueryFilter(t => t.TenantId == _tenantContext.CurrentTenant.Value);
    }

    #region Identity

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Agent>(agent =>
        {
            agent.ToTable("agents");
            agent.HasKey(a => a.Id);
            agent.Property(a => a.Id).HasConversion(id => id.Value, value => new AgentId(value));
            agent.Property(a => a.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            agent.Property(a => a.ExternalId).IsRequired().HasMaxLength(255);
            agent.Property(a => a.Name).IsRequired().HasMaxLength(255);
            agent.Property(a => a.Framework).HasConversion<string>().HasMaxLength(100);
            agent.Property(a => a.ModelProvider).HasConversion<string>().HasMaxLength(100);
            agent.Property(a => a.ModelName).IsRequired().HasMaxLength(100);
            agent.Property(a => a.Status).HasConversion<string>().HasMaxLength(50);
            agent.Property(a => a.CapabilityManifest)
                .HasColumnType("jsonb")
                .HasConversion(
                    manifest => JsonSerializer.Serialize(manifest, CapabilityManifestContext.Default.CapabilityManifest),
                    json => JsonSerializer.Deserialize<CapabilityManifest>(json, CapabilityManifestContext.Default.CapabilityManifest)!);
            agent.Property(a => a.CapabilityManifestHash).IsRequired().HasMaxLength(64);
            agent.HasIndex(a => new { a.TenantId, a.ExternalId }).IsUnique();
            agent.HasIndex(a => new { a.TenantId, a.Status });
            agent.HasIndex(a => new { a.TenantId, a.Framework });
            agent.HasIndex(a => a.IdentityTokenJti);
            agent.HasIndex(a => a.CapabilityManifest).HasMethod("GIN");
        });

        modelBuilder.Entity<AgentTrustRelationship>(rel =>
        {
            rel.ToTable("agent_trust_relationships");
            rel.HasKey(r => r.Id);
            rel.Property(r => r.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            rel.Property(r => r.SourceAgentId).HasConversion(id => id.Value, value => new AgentId(value));
            rel.Property(r => r.TargetAgentId).HasConversion(id => id.Value, value => new AgentId(value));
            rel.Property(r => r.RelationshipType).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);
            user.Property(u => u.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            user.Property(u => u.Email).IsRequired().HasMaxLength(255);
            user.Property(u => u.Name).IsRequired().HasMaxLength(255);
            user.Property(u => u.PasswordHash).HasMaxLength(255);
            user.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);
            user.Property(u => u.SsoProvider).HasMaxLength(50);
            user.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            user.HasIndex(u => new { u.TenantId, u.Role });
        });

        modelBuilder.Entity<UserSession>(session =>
        {
            session.ToTable("user_sessions");
            session.HasKey(s => s.Id);
            session.Property(s => s.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            session.Property(s => s.TokenHash).IsRequired().HasMaxLength(255);
            session.Property(s => s.RefreshTokenHash).HasMaxLength(255);
            session.Property(s => s.RevokedReason).HasMaxLength(255);
            session.Property(s => s.DeviceFingerprint).HasMaxLength(255);
            session.HasIndex(s => s.RefreshTokenHash).IsUnique();
            session.HasIndex(s => new { s.UserId, s.CreatedAt });
            session.HasIndex(s => s.ExpiresAt).HasFilter("\"RevokedAt\" IS NULL");
        });
    }

    #endregion

    #region Security

    private static void ConfigureSecurity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecurityPolicy>(entity =>
        {
            entity.ToTable("security_policies");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasConversion(id => id.Value, value => new SecurityPolicyId(value));
            entity.Property(p => p.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Description).IsRequired().HasMaxLength(1000);
            entity.Property(p => p.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(p => p.Severity).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.RuleDefinition).HasConversion<RuleDefinitionConverter>().HasColumnType("jsonb");
            entity.HasIndex(p => new { p.TenantId, p.Status });
            entity.HasIndex(p => p.TenantId);
        });

        modelBuilder.Entity<SecurityViolation>(entity =>
        {
            entity.ToTable("security_violations");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasColumnName("Id").HasConversion(id => id.Value, value => new SecurityViolationId(value));
            entity.Property(v => v.PolicyId).HasColumnName("PolicyId").HasConversion(id => id.Value, value => new SecurityPolicyId(value));
            entity.Property(v => v.TenantId).HasColumnName("TenantId").HasConversion(id => id.Value, value => new TenantId(value));
            entity.Property(v => v.AgentId).HasColumnName("AgentId").HasConversion(id => id.Value, value => new AgentId(value));
            entity.Property(v => v.TraceId).HasColumnName("TraceId").HasConversion(id => id.Value, value => new TraceId(value));
            entity.Property(v => v.Severity).HasColumnName("Severity").HasConversion<string>().HasMaxLength(20);
            entity.Property(v => v.Status).HasColumnName("Status").HasConversion<string>().HasMaxLength(20);
            entity.Property(v => v.Description).HasColumnName("Description").IsRequired().HasMaxLength(1000);
            entity.Property(v => v.TriggeringRule).HasColumnName("TriggeringRule").HasConversion<RuleDefinitionConverter>().HasColumnType("jsonb");
            entity.Property(v => v.ResolvedAt).HasColumnName("ResolvedAt");
            entity.Property(v => v.ResolvedBy).HasColumnName("ResolvedBy");
            entity.Property(v => v.ResolutionNotes).HasColumnName("ResolutionNotes").HasMaxLength(1000);
            entity.Property(v => v.CreatedAt).HasColumnName("CreatedAt");
            entity.Property(v => v.UpdatedAt).HasColumnName("UpdatedAt");
            entity.HasIndex(v => v.TenantId);
            entity.HasIndex(v => v.PolicyId);
            entity.HasIndex(v => v.AgentId);
            entity.HasIndex(v => v.CreatedAt);
        });
    }

    #endregion

    #region Observability

    private static void ConfigureObservability(ModelBuilder modelBuilder)
    {
        var builder = modelBuilder.Entity<TraceEntity>();
        builder.ToTable("traces");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.AgentId).HasColumnName("agent_id").IsRequired();
        builder.Property(t => t.ParentTraceId).HasColumnName("parent_trace_id");
        builder.Property(t => t.Framework).HasColumnName("framework").HasMaxLength(100);
        builder.Property(t => t.ModelProvider).HasColumnName("model_provider").HasMaxLength(100);
        builder.Property(t => t.ModelName).HasColumnName("model_name").HasMaxLength(100);
        builder.Property(t => t.OperationName).HasColumnName("operation_name").HasMaxLength(255);
        builder.Property(t => t.OperationType).HasColumnName("operation_type").HasMaxLength(50);
        builder.Property(t => t.StartTime).HasColumnName("start_time");
        builder.Property(t => t.EndTime).HasColumnName("end_time");
        builder.Property(t => t.DurationMs).HasColumnName("duration_ms");
        builder.Property(t => t.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(t => t.StatusMessage).HasColumnName("status_message");
        builder.Property(t => t.Attributes).HasColumnName("attributes").HasColumnType("jsonb");
        builder.Property(t => t.Events).HasColumnName("events").HasColumnType("jsonb");
        builder.Property(t => t.Links).HasColumnName("links").HasColumnType("jsonb");
        builder.Property(t => t.TokenUsageInput).HasColumnName("token_usage_input");
        builder.Property(t => t.TokenUsageOutput).HasColumnName("token_usage_output");
        builder.Property(t => t.TokenUsageTotal).HasColumnName("token_usage_total");
        builder.Property(t => t.TokenUsageCached).HasColumnName("token_usage_cached");
        builder.Property(t => t.CostUsd).HasColumnName("cost_usd").HasPrecision(18, 8);
        builder.Property(t => t.CostCurrency).HasColumnName("cost_currency").HasMaxLength(10);
        builder.Property(t => t.LatencyMs).HasColumnName("latency_ms");
        builder.Property(t => t.QueueTimeMs).HasColumnName("queue_time_ms");
        builder.Property(t => t.IsRootSpan).HasColumnName("is_root_span");
        builder.Property(t => t.IsAsync).HasColumnName("is_async");
        builder.Property(t => t.TraceFlags).HasColumnName("trace_flags");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(t => new { t.TenantId, t.StartTime });
        builder.HasIndex(t => new { t.AgentId, t.StartTime });
        builder.HasIndex(t => t.Id);
    }

    #endregion

    #region Cost Intelligence

    private static void ConfigureCostIntelligence(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostBudget>(builder =>
        {
            builder.ToTable("cost_budgets");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasConversion(id => id.Value, value => new CostBudgetId(value));
            builder.Property(b => b.TenantId).HasConversion(id => id.Value, value => new TenantId(value));
            builder.Property(b => b.Name).IsRequired().HasMaxLength(255);
            builder.Property(b => b.ScopeType).HasConversion<string>().HasMaxLength(50);
            builder.Property(b => b.ScopeId).IsRequired();
            builder.Property(b => b.BudgetAmount)
                .HasColumnName("budget_amount_usd")
                .HasColumnType("decimal(18, 8)")
                .HasConversion(new MoneyAmountConverter());
            builder.Property(b => b.CurrentSpend)
                .HasColumnName("current_spend_usd")
                .HasColumnType("decimal(18, 8)")
                .HasConversion(new MoneyAmountConverter());
            builder.Property(b => b.Period).HasConversion<string>().HasMaxLength(50);
            builder.Property(b => b.AlertThresholds)
                .HasColumnType("jsonb")
                .HasConversion(
                    list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<IReadOnlyList<decimal>>(json, (JsonSerializerOptions?)null) ?? Array.Empty<decimal>())
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<decimal>>(
                    (a, b) => a!.SequenceEqual(b!),
                    v => v.Aggregate(0, (hash, x) => HashCode.Combine(hash, x)),
                    v => (IReadOnlyList<decimal>)v.ToList()));
            builder.Property(b => b.EnforcementType).HasConversion<string>().HasMaxLength(50);
            builder.Property(b => b.LastResetAt).IsRequired();
            builder.Property(b => b.CreatedBy).IsRequired();
            builder.HasIndex(b => new { b.TenantId, b.ScopeType, b.ScopeId, b.Period }).IsUnique();
            builder.HasIndex(b => new { b.TenantId, b.ScopeType });
        });
    }

    #endregion

    #region Governance

    private static void ConfigureGovernance(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GovernancePolicy>(e =>
        {
            e.ToTable("app_governance_policies");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasConversion(id => id.Value, v => new SeldonVigil.Api.Modules.Governance.Domain.ValueObjects.GovernancePolicyId(v));
            e.Property(p => p.TenantId).HasConversion(id => id.Value, v => new TenantId(v));
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.Category).HasConversion<string>().HasMaxLength(50);
        });

        modelBuilder.Entity<CertificationPipeline>(e =>
        {
            e.ToTable("app_certification_pipelines");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasConversion(id => id.Value, v => new SeldonVigil.Api.Modules.Governance.Domain.ValueObjects.CertificationPipelineId(v));
            e.Property(p => p.TenantId).HasConversion(id => id.Value, v => new TenantId(v));
            e.Property(p => p.AgentId).HasConversion(id => id.Value, v => new AgentId(v));
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
            ConfigureJson(e.Property(p => p.Stages));
        });
    }

    #endregion

    #region AI Reasoning

    private static void ConfigureAIReasoning(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RootCauseAnalysis>(e =>
        {
            e.ToTable("app_root_cause_analyses");
            e.HasKey(r => r.Id);
            e.Property(r => r.IsDeleted).HasColumnName("is_deleted");
        });
    }

    #endregion

    #region Analytics

    private static void ConfigureAnalytics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("app_notifications");
            e.HasKey(n => n.Id);
            e.Property(n => n.IsDeleted).HasColumnName("is_deleted");
        });
    }

    #endregion

    #region Integration

    private static void ConfigureIntegration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationConfiguration>(e =>
        {
            e.ToTable("app_integrations");
            e.HasKey(i => i.Id);
            e.Property(i => i.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(40);
            e.Property(i => i.IsDeleted).HasColumnName("is_deleted");
        });
    }

    #endregion

    #region Domain

    private static void ConfigureDomain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromptVersionEntity>(e =>
        {
            e.ToTable("app_prompt_versions");
            e.HasKey(v => v.Id);
            e.Property(v => v.Name).HasMaxLength(255).IsRequired();
            e.Property(v => v.Template).IsRequired();
            e.Property(v => v.SystemPrompt).IsRequired();
            e.Property(v => v.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(v => v.CreatedAt).HasColumnType("timestamp without time zone");
            e.Property(v => v.UpdatedAt).HasColumnType("timestamp without time zone");
            ConfigureJson(e.Property(v => v.ModelConfig));

            e.HasIndex(v => new { v.Name, v.Version }).IsUnique();
            e.HasIndex(v => new { v.Name, v.Status });
        });

        modelBuilder.Entity<EvaluationResultEntity>(e =>
        {
            e.ToTable("app_evaluation_results");
            e.HasKey(r => r.Id);
            e.Property(r => r.PromptName).HasMaxLength(255).IsRequired();
            e.Property(r => r.EvaluatedAt).HasColumnType("timestamp without time zone");
            ConfigureJson(e.Property(r => r.Metrics));

            e.HasIndex(r => new { r.PromptName, r.EvaluatedAt });
        });
    }

    #endregion

    #region Saga

    private static void ConfigureSaga(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SagaInstance>(entity =>
        {
            entity.ToTable("saga_instances");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.SagaType).HasMaxLength(255).IsRequired();
            entity.Property(s => s.CorrelationId).HasMaxLength(255).IsRequired();
            entity.Property(s => s.State).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(s => s.SagaData).HasColumnType("jsonb");
            entity.Property(s => s.ErrorMessage).HasMaxLength(4000);

            entity.HasIndex(s => s.CorrelationId);
            entity.HasIndex(s => s.State);
            entity.HasIndex(s => new { s.State, s.RetryCount });
        });
    }

    #endregion

    #region Outbox

    private static void ConfigureOutbox(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Payload).HasColumnType("jsonb");
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);

            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => new { e.ProcessedAt, e.CreatedAt });
        });
    }

    #endregion

    #region Helpers

    private static void ConfigureJson<T>(PropertyBuilder<T> builder) where T : class
    {
        _ = builder.HasColumnType("jsonb");
        _ = builder.HasConversion(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => FromJson<T>(json));
    }

    private static T FromJson<T>(string json) where T : class
    {
        var result = JsonSerializer.Deserialize<T>(json, (JsonSerializerOptions?)null);
        return result ?? throw new InvalidOperationException("Failed to deserialize jsonb value.");
    }

    private sealed class MoneyAmountConverter() : ValueConverter<Money, decimal>(money => money.Amount,
        amount => new Money(amount, "USD"));

    private sealed class RuleDefinitionConverter() : ValueConverter<RuleDefinition, string>(rd => Serialize(rd),
        json => Deserialize(json))
    {
        [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
        private static string Serialize(RuleDefinition rd)
        {
            return JsonSerializer.Serialize(new
            {
                type = rd.Type,
                expression = rd.Expression,
                parameters = rd.Parameters
            });
        }

        [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(String, JsonSerializerOptions)")]
        private static RuleDefinition Deserialize(string json)
        {
            var doc = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (doc == null) return null!;

            if (doc.TryGetValue("type", out var typeValue))
            {
                var parameters = doc.TryGetValue("parameters", out var paramsValue)
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(paramsValue.ToString() ?? "{}")!
                    : new Dictionary<string, object>();

                return new RuleDefinition
                {
                    Type = Enum.Parse<RuleType>(typeValue.ToString()!, ignoreCase: true),
                    Expression = doc.TryGetValue("expression", out var expressionValue)
                        ? expressionValue.ToString() ?? string.Empty
                        : string.Empty,
                    Parameters = parameters
                };
            }

            return FromLegacyFlatShape(doc);
        }

        private static RuleDefinition FromLegacyFlatShape(Dictionary<string, object> doc)
        {
            var metric = doc.GetValueOrDefault("metric")?.ToString();
            var op = doc.GetValueOrDefault("operator")?.ToString();
            var hasValue = doc.TryGetValue("value", out var valueObj);

            if (string.Equals(op, "contains_pattern", StringComparison.OrdinalIgnoreCase))
            {
                return new RuleDefinition
                {
                    Type = RuleType.RegexMatch,
                    Expression = "contains",
                    Parameters = new Dictionary<string, object>
                    {
                        ["field"] = metric ?? "operation_name",
                        ["pattern"] = hasValue ? valueObj?.ToString() ?? string.Empty : string.Empty,
                    }
                };
            }

            return new RuleDefinition
            {
                Type = RuleType.Threshold,
                Expression = op ?? string.Empty,
                Parameters = new Dictionary<string, object>
                {
                    ["metric"] = metric ?? string.Empty,
                    ["operator"] = MapLegacyOperator(op),
                    ["value"] = hasValue ? valueObj! : 0,
                }
            };
        }

        private static string MapLegacyOperator(string? op) => op switch
        {
            "greater_than" => "gt",
            "greater_than_or_equal" => "gte",
            "less_than" => "lt",
            "less_than_or_equal" => "lte",
            "equal" => "eq",
            _ => op ?? string.Empty
        };
    }

    #endregion
}
