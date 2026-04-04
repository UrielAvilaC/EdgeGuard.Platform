using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Configurations;

public class NodePacsAssignmentConfiguration : IEntityTypeConfiguration<NodePacsAssignment>
{
    public void Configure(EntityTypeBuilder<NodePacsAssignment> builder)
    {
        builder.ToTable("NodePacsAssignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(50);
        builder.Property(a => a.NodeId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PacsId).IsRequired().HasMaxLength(50);

        builder.HasIndex(a => new { a.NodeId, a.PacsId, a.IsActive });
    }
}
