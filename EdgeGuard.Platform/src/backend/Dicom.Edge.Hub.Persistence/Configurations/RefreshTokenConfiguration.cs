using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(50);
        builder.Property(t => t.UserId).IsRequired().HasMaxLength(50);
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(88);
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(88);

        builder.HasIndex(t => t.TokenHash);
        builder.HasIndex(t => t.UserId);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsRevoked);
        builder.Ignore(t => t.IsActive);
    }
}
