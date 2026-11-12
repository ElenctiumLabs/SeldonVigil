using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Shared.Infrastructure.Tenancy;

namespace SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence;

[method: RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
[method: RequiresDynamicCode("Calls Microsoft.EntityFrameworkCore.DbContext.DbContext(DbContextOptions)")]
internal sealed class CostIntelligenceDbContext(
    DbContextOptions<CostIntelligenceDbContext> options,
    ITenantContext tenantContext)
    : DbContext(options)
{
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<CostBudget> CostBudgets => Set<CostBudget>();

    [RequiresUnreferencedCode("Calls Microsoft.EntityFrameworkCore.ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func<Type, Boolean>)")]
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(CostIntelligenceDbContext).Assembly,
            t => t.Namespace?.StartsWith("SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence.Configurations") == true);

        // DDD: domain events are transient, not persisted as entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => entityType.FindNavigation("DomainEvents") is not null))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore("DomainEvents");
        }

        // Tenant isolation: fail closed when no tenant is resolved.
        
        _ = modelBuilder.Entity<CostBudget>().HasQueryFilter(b => b.TenantId.Equals(_tenantContext.CurrentTenant));
    }
}
