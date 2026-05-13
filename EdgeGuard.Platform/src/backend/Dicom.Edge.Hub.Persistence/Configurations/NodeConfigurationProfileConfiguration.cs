using Dicom.Edge.Hub.Domain.Aggregates.NodeConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class NodeConfigurationProfileConfiguration
    : IEntityTypeConfiguration<NodeConfigurationProfile>
{
    public void Configure(EntityTypeBuilder<NodeConfigurationProfile> builder)
    {
        builder.ToTable("node_configuration_profiles");

        builder.HasKey(p => new { p.NodeId, p.SettingKey });

        builder.Property(p => p.NodeId).HasMaxLength(128).IsRequired();
        builder.Property(p => p.SettingKey).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Value).HasMaxLength(4000).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(50).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.ValueType).HasMaxLength(20).IsRequired();

        builder.HasIndex(p => p.NodeId);
        builder.HasIndex(p => new { p.NodeId, p.Category });
    }
}
