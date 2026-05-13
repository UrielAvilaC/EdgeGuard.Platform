namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class DicomAssociationConfiguration : IEntityTypeConfiguration<DicomAssociation>
{
    public void Configure(EntityTypeBuilder<DicomAssociation> b)
    {
        b.ToTable(TableNames.DicomAssociations);
        b.HasKey(a => a.Id);

        b.Property(a => a.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(a => a.CallingAeTitle).HasColumnName("calling_ae_title").HasMaxLength(16).IsRequired();
        b.Property(a => a.CalledAeTitle).HasColumnName("called_ae_title").HasMaxLength(16).IsRequired();
        b.Property(a => a.RemoteIpAddress).HasColumnName("remote_ip").HasMaxLength(64).IsRequired();
        b.Property(a => a.RemotePort).HasColumnName("remote_port");
        b.Property(a => a.Status).HasColumnName("status").HasConversion<int>();
        b.Property(a => a.ConnectedAt).HasColumnName("connected_at").IsRequired();
        b.Property(a => a.DisconnectedAt).HasColumnName("disconnected_at");
        b.Property(a => a.ImagesReceived).HasColumnName("images_received").HasDefaultValue(0);
        b.Property(a => a.BytesReceived).HasColumnName("bytes_received").HasDefaultValue(0L);
        b.Property(a => a.RejectionReason).HasColumnName("rejection_reason");
        b.Property(a => a.EdgeNodeId).HasColumnName("edge_node_id").HasMaxLength(64);
        b.Property(a => a.AcceptedPresentationContexts).HasColumnName("accepted_contexts");

        b.HasIndex(a => a.CallingAeTitle).HasDatabaseName(IndexNames.AssociationCallingAe);
        b.HasIndex(a => a.ConnectedAt).HasDatabaseName(IndexNames.AssociationConnected);
        b.HasIndex(a => a.Status).HasDatabaseName(IndexNames.AssociationStatus);
    }
}
