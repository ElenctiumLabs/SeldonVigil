using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeldonVigil.Api.Modules.Identity.Domain.Entities;
using SeldonVigil.Api.Modules.Identity.Domain.ValueObjects;

namespace SeldonVigil.Api.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.TenantId)
            .HasConversion(
                id => id.Value,
                value => new TenantId(value));

        builder.Property(s => s.TokenHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.RefreshTokenHash)
            .HasMaxLength(255);

        builder.Property(s => s.RevokedReason)
            .HasMaxLength(255);

        builder.Property(s => s.DeviceFingerprint)
            .HasMaxLength(255);

        builder.HasIndex(s => s.RefreshTokenHash)
            .IsUnique();

        builder.HasIndex(s => new { s.UserId, s.CreatedAt });
        builder.HasIndex(s => s.ExpiresAt)
            .HasFilter("\"RevokedAt\" IS NULL");
    }
}
