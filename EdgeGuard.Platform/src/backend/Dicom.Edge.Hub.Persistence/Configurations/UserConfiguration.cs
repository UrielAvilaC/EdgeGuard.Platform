using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasMaxLength(50);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.FailedLoginAttempts).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(50);

        builder.HasIndex(u => u.Username).IsUnique();

        builder.HasMany(u => u.Roles)
               .WithOne()
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.DirectPermissions)
               .WithOne()
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
               .WithOne()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(u => u.IsLocked);
        builder.Ignore(u => u.DomainEvents);
    }
}
