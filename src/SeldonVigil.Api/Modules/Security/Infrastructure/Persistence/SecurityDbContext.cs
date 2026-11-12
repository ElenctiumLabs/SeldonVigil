using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Observability.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Security.Domain.Entities;
using SeldonVigil.Api.Modules.Security.Domain.ValueObjects;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SeldonVigil.Api.Modules.Security.Infrastructure.Persistence;

[method: RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
[method: RequiresDynamicCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
internal sealed class SecurityDbContext(
    DbContextOptions<SecurityDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<SecurityPolicy> SecurityPolicies => Set<SecurityPolicy>();
    public DbSet<SecurityViolation> SecurityViolations => Set<SecurityViolation>();

    [RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func<Type, Boolean>)")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SecurityDbContext).Assembly,
            t => t.Namespace?.StartsWith("SeldonVigil.Api.Modules.Security.Infrastructure.Persistence.Configurations") == true);

        // DDD: domain events are transient, not persisted as entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => entityType.FindNavigation("DomainEvents") is not null))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }

        // Configure SecurityPolicy entity
        modelBuilder.Entity<SecurityPolicy>(entity =>
        {
            entity.ToTable("security_policies");
            entity.HasKey(p => p.Id);

            entity.Ignore(p => p.DomainEvents);

            entity.Property(p => p.Id)
                .HasConversion(
                    id => id.Value,
                    value => new SecurityPolicyId(value));

            entity.Property(p => p.TenantId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantId(value));

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(p => p.Category)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.Property(p => p.Severity)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(p => p.CreatedBy);

            // Configure RuleDefinition as JSON
            entity.Property(p => p.RuleDefinition)
                .HasConversion<RuleDefinitionConverter>()
                .HasColumnType("jsonb");

            entity.HasIndex(p => new { p.TenantId, p.Status });
            entity.HasIndex(p => p.TenantId);
        });

        // Configure SecurityViolation entity
        modelBuilder.Entity<SecurityViolation>(entity =>
        {
            entity.ToTable("security_violations");
            entity.HasKey(v => v.Id);

            entity.Ignore(v => v.DomainEvents);

            entity.Property(v => v.Id)
                .HasConversion(
                    id => id.Value,
                    value => new SecurityViolationId(value));

            entity.Property(v => v.PolicyId)
                .HasConversion(
                    id => id.Value,
                    value => new SecurityPolicyId(value));

            entity.Property(v => v.TenantId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantId(value));

            entity.Property(v => v.AgentId)
                .HasConversion(
                    id => id.Value,
                    value => new AgentId(value));

            entity.Property(v => v.TraceId)
                .HasConversion(
                    id => id.Value,
                    value => new TraceId(value));

            entity.Property(v => v.Severity)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(v => v.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(v => v.Description)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(v => v.TriggeringRule)
                .HasConversion<RuleDefinitionConverter>()
                .HasColumnType("jsonb");

            entity.Property(v => v.ResolvedBy);
            entity.Property(v => v.ResolutionNotes)
                .HasMaxLength(1000);

            entity.HasIndex(v => v.TenantId);
            entity.HasIndex(v => v.PolicyId);
            entity.HasIndex(v => v.AgentId);
            entity.HasIndex(v => v.CreatedAt);
        });

        // Tenant isolation: fail closed when no tenant is resolved.
        // Empty tenant never matches any row - fail closed (VO compared directly
        // so the configured value converter applies).
        
        _ = modelBuilder.Entity<SecurityPolicy>().HasQueryFilter(p => p.TenantId.Equals(_tenantContext.CurrentTenant));
        _ = modelBuilder.Entity<SecurityViolation>().HasQueryFilter(v => v.TenantId.Equals(_tenantContext.CurrentTenant));
    }

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
            if (doc == null)
            {
                return null!;
            }

            // Canonical shape: { type, expression, parameters }
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

            // Legacy flat shape (early seed data): { metric, operator, value, ... }
            return FromLegacyFlatShape(doc);
        }

        private static RuleDefinition FromLegacyFlatShape(Dictionary<string, object> doc)
        {
            var metric = doc.GetValueOrDefault("metric")?.ToString();
            var op = doc.GetValueOrDefault("operator")?.ToString();
            var hasValue = doc.TryGetValue("value", out var valueObj);

            // "contains_pattern" rules match a pattern against the named trace field.
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

            // Everything else is treated as a numeric comparison against the metric.
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
}
