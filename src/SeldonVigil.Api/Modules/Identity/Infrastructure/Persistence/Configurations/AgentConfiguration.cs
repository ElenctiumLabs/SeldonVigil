using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("agents");

        builder.HasKey(a => a.Id);

        builder.Ignore(a => a.DomainEvents);

        builder.Property(a => a.Id)
            .HasConversion(
                id => id.Value,
                value => new AgentId(value));

        builder.Property(a => a.TenantId)
            .HasConversion(
                id => id.Value,
                value => new TenantId(value));

        builder.Property(a => a.ExternalId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.Framework)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(a => a.ModelProvider)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(a => a.ModelName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.CapabilityManifest)
            .HasColumnType("jsonb")
            .HasConversion(
                manifest => JsonSerializer.Serialize(manifest, CapabilityManifestContext.Default.CapabilityManifest),
                json => JsonSerializer.Deserialize<CapabilityManifest>(json, CapabilityManifestContext.Default.CapabilityManifest)!);

        builder.Property(a => a.CapabilityManifestHash)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(a => new { a.TenantId, a.ExternalId })
            .IsUnique();

        builder.HasIndex(a => new { a.TenantId, a.Status });
        builder.HasIndex(a => new { a.TenantId, a.Framework });
        builder.HasIndex(a => a.IdentityTokenJti);

        // GIN index for JSONB
        builder.HasIndex(a => a.CapabilityManifest)
            .HasMethod("GIN");
    }
}
