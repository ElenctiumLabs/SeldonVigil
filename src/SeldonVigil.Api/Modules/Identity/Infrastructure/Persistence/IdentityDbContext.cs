using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence;

[method: RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
[method: RequiresDynamicCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
internal sealed class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<AgentTrustRelationship> AgentTrustRelationships => Set<AgentTrustRelationship>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    [RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func<Type, Boolean>)")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IdentityDbContext).Assembly,
            t => t.Namespace?.StartsWith("SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence.Configurations") == true);

        // DDD: domain events are transient, not persisted as entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => entityType.FindNavigation("DomainEvents") is not null))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }

        // Tenant isolation: global query filters evaluated per query against the
        // request's resolved tenant (JWT claim first). Null tenant => matches
        // nothing (fail closed).
        // Tenant isolation. IMPORTANT: the filter must reference the injected
        // service property (_tenantContext.CurrentTenant), NOT a local copy -
        // locals are baked into the cached EF model (pooled contexts share one
        // model) and freeze forever. Service properties are evaluated per query.
        // CurrentTenant defaults to TenantId.Empty, which matches no rows - fail closed.
        _ = modelBuilder.Entity<Agent>().HasQueryFilter(a => a.TenantId.Equals(_tenantContext.CurrentTenant));
        _ = modelBuilder.Entity<AgentTrustRelationship>().HasQueryFilter(t => t.TenantId.Equals(_tenantContext.CurrentTenant));
        _ = modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId.Equals(_tenantContext.CurrentTenant));
        _ = modelBuilder.Entity<UserSession>().HasQueryFilter(s => s.TenantId.Equals(_tenantContext.CurrentTenant));
    }
}
