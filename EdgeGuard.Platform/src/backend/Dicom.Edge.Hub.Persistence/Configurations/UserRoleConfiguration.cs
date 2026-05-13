using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(50);
        builder.Property(r => r.UserId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Role).IsRequired();
        builder.Property(r => r.AssignedBy).HasMaxLength(50);

        builder.HasIndex(r => new { r.UserId, r.Role }).IsUnique();
    }
}
