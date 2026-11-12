using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeldonVigil.Api.Modules.AIReasoning.Domain.Entities;
using SeldonVigil.Api.Modules.Analytics.Domain.Entities;
using SeldonVigil.Api.Modules.Governance.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Integration.Domain.Entities;

namespace SeldonVigil.Api.Shared.Infrastructure.Persistence;

/// <summary>
/// Persistence for the formerly in-memory module stores (governance policies,
/// certification pipelines, root-cause analyses, notifications, integrations).
///
/// Schema strategy: this context owns its app_* tables and is created with
/// EnsureCreated at startup - deliberately NOT part of the migration set.
/// Repositories filter tenants explicitly by parameter, matching the previous
/// in-memory behaviour.
/// </summary>
internal sealed class AppDataDbContext : DbContext
{
    public AppDataDbContext(DbContextOptions<AppDataDbContext> options) : base(options) { }

    public DbSet<GovernancePolicy> GovernancePolicies => Set<GovernancePolicy>();
    public DbSet<CertificationPipeline> CertificationPipelines => Set<CertificationPipeline>();
    public DbSet<RootCauseAnalysis> RootCauseAnalyses => Set<RootCauseAnalysis>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IntegrationConfiguration> Integrations => Set<IntegrationConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GovernancePolicy>(e =>
        {
            e.ToTable("app_governance_policies");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasConversion(id => id.Value, v => new SeldonVigil.Api.Modules.Governance.Domain.ValueObjects.GovernancePolicyId(v));
            e.Property(p => p.TenantId).HasConversion(id => id.Value, v => new TenantId(v));
            e.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
            e.Property(p => p.Category).HasConversion<string>().HasMaxLength(50);
            e.Ignore(p => p.DomainEvents);
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
            e.Ignore(p => p.DomainEvents);
        });

        modelBuilder.Entity<RootCauseAnalysis>(e =>
        {
            e.ToTable("app_root_cause_analyses");
            e.HasKey(r => r.Id);
            e.Property(r => r.IsDeleted).HasColumnName("is_deleted");
            e.Ignore(r => r.DomainEvents);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("app_notifications");
            e.HasKey(n => n.Id);
            e.Property(n => n.IsDeleted).HasColumnName("is_deleted");
            e.Ignore(n => n.DomainEvents);
        });

        modelBuilder.Entity<IntegrationConfiguration>(e =>
        {
            e.ToTable("app_integrations");
            e.HasKey(i => i.Id);
            e.Property(i => i.Type).HasConversion<string>().HasMaxLength(40);
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(40);
            e.Property(i => i.IsDeleted).HasColumnName("is_deleted");
            e.Ignore(i => i.DomainEvents);
        });
    }

    private static void ConfigureJson<T>(PropertyBuilder<T> builder) where T : class
    {
        // Must stay in sync with the raw bootstrap DDL in Program.cs, which
        // creates these columns as jsonb. Without the explicit column type,
        // Npgsql binds the serialized string as text and PostgreSQL rejects
        // the write with 42804 whenever the raw DDL created the table first.
        _ = builder.HasColumnType("jsonb");
        _ = builder.HasConversion(
            list => System.Text.Json.JsonSerializer.Serialize(list, (System.Text.Json.JsonSerializerOptions?)null),
            json => FromJson<T>(json));
    }

    private static T FromJson<T>(string json) where T : class
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<T>(json, (System.Text.Json.JsonSerializerOptions?)null);
        return result ?? throw new InvalidOperationException("Failed to deserialize jsonb value.");
    }
}
