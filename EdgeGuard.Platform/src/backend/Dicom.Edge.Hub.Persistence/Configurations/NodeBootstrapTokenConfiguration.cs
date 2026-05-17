using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NodeBootstrapTokenConfiguration : IEntityTypeConfiguration<NodeBootstrapToken>
{
    public void Configure(EntityTypeBuilder<NodeBootstrapToken> builder)
    {
        builder.ToTable("node_bootstrap_tokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(50);
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(t => t.CreatedByUserId).HasMaxLength(50);
        builder.Property(t => t.ConsumedByNodeId).HasMaxLength(50);
        builder.Property(t => t.Note).HasMaxLength(200);

        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.IsConsumed);
        builder.HasIndex(t => t.ExpiresAt);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsValid);
    }
}
