using System.Diagnostics.CodeAnalysis;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace SeldonVigil.Api.Modules.Observability.Infrastructure.Persistence;

[method: RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
[method: RequiresDynamicCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
internal sealed class ObservabilityDbContext(
    DbContextOptions<ObservabilityDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<TraceEntity> Traces => Set<TraceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
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

        // Tenant isolation: fail closed when no tenant is resolved.
        
        _ = builder.HasQueryFilter(t => t.TenantId == _tenantContext.CurrentTenant.Value);
    }
}
