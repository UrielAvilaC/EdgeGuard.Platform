using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasMaxLength(100).HasColumnName("key");
        builder.Property(s => s.Value).HasMaxLength(4000).IsRequired();
        builder.Property(s => s.Category).HasMaxLength(50).IsRequired();
        builder.Property(s => s.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ValueType).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);

        builder.HasIndex(s => s.Category);
    }
}
