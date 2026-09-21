using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NodePacsAssignmentConfiguration : IEntityTypeConfiguration<NodePacsAssignment>
{
    public void Configure(EntityTypeBuilder<NodePacsAssignment> builder)
    {
        builder.ToTable("node_pacs_assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(50);
        builder.Property(a => a.NodeId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PacsId).IsRequired().HasMaxLength(50);

        // Último resultado C-ECHO de este par (nodo, PACS). El error se acota porque es
        // texto de diagnóstico para la pantalla, no una traza: el detalle completo del
        // momento vive en el store en vivo.
        builder.Property(a => a.LastCEchoError).HasMaxLength(512);
        builder.Property(a => a.LastCEchoErrorReason).HasMaxLength(128);

        builder.HasIndex(a => new { a.NodeId, a.PacsId, a.IsActive });
    }
}
