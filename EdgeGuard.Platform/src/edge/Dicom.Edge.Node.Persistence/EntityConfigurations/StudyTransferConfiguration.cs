namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class StudyTransferConfiguration : IEntityTypeConfiguration<StudyTransfer>
{
    public void Configure(EntityTypeBuilder<StudyTransfer> b)
    {
        b.ToTable(TableNames.StudyTransfers);
        b.HasKey(t => t.Id);

        b.Property(t => t.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(t => t.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(t => t.EdgeNodeId).HasColumnName("edge_node_id").HasMaxLength(64);
        b.Property(t => t.Status).HasColumnName("status").HasConversion<int>();
        b.Property(t => t.StartedAt).HasColumnName("started_at").IsRequired();
        b.Property(t => t.CompletedAt).HasColumnName("completed_at");
        b.Property(t => t.TotalSizeBytes).HasColumnName("total_size_bytes").HasDefaultValue(0L);
        b.Property(t => t.BytesTransferred).HasColumnName("bytes_transferred").HasDefaultValue(0L);
        b.Property(t => t.TotalInstances).HasColumnName("total_instances").HasDefaultValue(0);
        b.Property(t => t.InstancesTransferred).HasColumnName("instances_xferred").HasDefaultValue(0);
        b.Property(t => t.InstancesFailed).HasColumnName("instances_failed").HasDefaultValue(0);
        b.Property(t => t.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        b.Property(t => t.ErrorMessage).HasColumnName("error_message");
        b.Property(t => t.TransferMethod).HasColumnName("transfer_method").HasMaxLength(64);
        b.Property(t => t.SourceAeTitle).HasColumnName("source_ae_title").HasMaxLength(16);
        b.Property(t => t.PatientId).HasColumnName("patient_id").HasMaxLength(64);
        b.Property(t => t.StudyDate).HasColumnName("study_date");
        b.Property(t => t.Modality).HasColumnName("modality").HasMaxLength(16);

        b.Ignore(t => t.IsSuccess);
        b.Ignore(t => t.ProgressPercent);

        b.HasIndex(t => t.StudyInstanceUid).HasDatabaseName(IndexNames.TransferStudy);
        b.HasIndex(t => new { t.Status, t.StartedAt }).HasDatabaseName(IndexNames.TransferStatus);
    }
}
