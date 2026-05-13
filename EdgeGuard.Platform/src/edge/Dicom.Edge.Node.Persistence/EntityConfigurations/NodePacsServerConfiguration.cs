using Dicom.Edge.Node.Persistence.Constants;
using Dicom.Edge.Node.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class NodePacsServerConfiguration : IEntityTypeConfiguration<NodePacsServer>
{
    public void Configure(EntityTypeBuilder<NodePacsServer> b)
    {
        b.ToTable(TableNames.NodePacsServers);
        b.HasKey(p => p.Id);

        b.Property(p => p.Id).HasColumnName("id").HasMaxLength(64).IsRequired();
        b.Property(p => p.Name).HasColumnName("name").HasMaxLength(128).IsRequired();
        b.Property(p => p.AeTitle).HasColumnName("ae_title").HasMaxLength(16).IsRequired();
        b.Property(p => p.Host).HasColumnName("host").HasMaxLength(256).IsRequired();
        b.Property(p => p.Port).HasColumnName("port").IsRequired();
        b.Property(p => p.Priority).HasColumnName("priority").HasDefaultValue(10);
        b.Property(p => p.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        b.Property(p => p.SyncedAt).HasColumnName("synced_at").IsRequired();
        b.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(p => p.UpdatedAt).HasColumnName("updated_at").IsRequired();

        b.HasIndex(p => p.AeTitle).HasDatabaseName("ix_node_pacs_servers_ae_title");
        b.HasIndex(p => p.IsEnabled).HasDatabaseName("ix_node_pacs_servers_is_enabled");
        b.HasIndex(p => p.Priority).HasDatabaseName("ix_node_pacs_servers_priority");
    }
}
