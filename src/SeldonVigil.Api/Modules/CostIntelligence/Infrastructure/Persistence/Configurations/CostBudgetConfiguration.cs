using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.Entities;
using SeldonVigil.Api.Modules.CostIntelligence.Domain.ValueObjects;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.CostIntelligence.Infrastructure.Persistence.Configurations;

internal sealed class CostBudgetConfiguration : IEntityTypeConfiguration<CostBudget>
{
    public void Configure(EntityTypeBuilder<CostBudget> builder)
    {
        builder.ToTable("cost_budgets");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasConversion(
                id => id.Value,
                value => new CostBudgetId(value));

        builder.Property(b => b.TenantId)
            .HasConversion(
                id => id.Value,
                value => new TenantId(value));

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(b => b.ScopeType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.ScopeId)
            .IsRequired();

        builder.Property(b => b.BudgetAmount)
            .HasColumnName("budget_amount_usd")
            .HasColumnType("decimal(18, 8)")
            .HasConversion(new MoneyAmountConverter());

        builder.Property(b => b.CurrentSpend)
            .HasColumnName("current_spend_usd")
            .HasColumnType("decimal(18, 8)")
            .HasConversion(new MoneyAmountConverter());

        builder.Property(b => b.Period)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.AlertThresholds)
            .HasColumnType("jsonb")
            .HasConversion(new AlertThresholdsConverter())
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<IReadOnlyList<decimal>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (hash, x) => HashCode.Combine(hash, x)),
                v => (IReadOnlyList<decimal>)v.ToList()));

        builder.Property(b => b.EnforcementType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(b => b.LastResetAt)
            .IsRequired();

        builder.Property(b => b.CreatedBy)
            .IsRequired();

        builder.HasIndex(b => new { b.TenantId, b.ScopeType, b.ScopeId, b.Period })
            .IsUnique();

        builder.HasIndex(b => new { b.TenantId, b.ScopeType });
    }

    // The schema stores a single currency column per budget (budget_currency).
    // Money values on the aggregate are persisted as their USD amount.
    // Currency passed explicitly: expression trees cannot use optional arguments
    // under C# 13 (preview-only relaxation in C# 14).
    private sealed class MoneyAmountConverter() : ValueConverter<Money, decimal>(money => money.Amount,
        amount => new Money(amount, "USD"));

    private sealed class AlertThresholdsConverter()
        : ValueConverter<IReadOnlyList<decimal>, string>(
            list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
            json => DeserializeThresholds(json))
    {
    }

    private static IReadOnlyList<decimal> DeserializeThresholds(string json)
    {
        var list = JsonSerializer.Deserialize<List<decimal>>(json, (JsonSerializerOptions?)null);
        return list is not null ? list.AsReadOnly() : new List<decimal>().AsReadOnly();
    }
}
