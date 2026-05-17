using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("user_permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(50);
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Permission).IsRequired();
        builder.Property(p => p.IsGranted).IsRequired();
        builder.Property(p => p.GrantedBy).HasMaxLength(50);

        builder.HasIndex(p => new { p.UserId, p.Permission }).IsUnique();
    }
}
