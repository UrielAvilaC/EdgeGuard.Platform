namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class StudyMetricsConfiguration : IEntityTypeConfiguration<StudyMetrics>
{
    public void Configure(EntityTypeBuilder<StudyMetrics> b)
    {
        b.ToTable(TableNames.StudyMetrics);
        b.HasKey(m => m.Id);

        b.Property(m => m.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(m => m.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(m => m.TotalSizeBytes).HasColumnName("total_size_bytes").HasDefaultValue(0L);
        b.Property(m => m.InstancesReceived).HasColumnName("instances_received").HasDefaultValue(0);
        b.Property(m => m.InstancesFailed).HasColumnName("instances_failed").HasDefaultValue(0);
        b.Property(m => m.AverageImageSize).HasColumnName("avg_image_size").HasDefaultValue(0.0);
        b.Property(m => m.FirstImageAt).HasColumnName("first_image_at");
        b.Property(m => m.LastImageAt).HasColumnName("last_image_at");
        b.Property(m => m.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);

        // TimeSpan → stored as total milliseconds (INTEGER)
        b.Property(m => m.ReceptionDuration)
         .HasColumnName("reception_ms")
         .HasConversion(
             ts => (long)ts.TotalMilliseconds,
             ms => TimeSpan.FromMilliseconds(ms));

        b.Property(m => m.TransferDuration)
         .HasColumnName("transfer_ms")
         .HasConversion(
             ts => ts.HasValue ? (long?)ts.Value.TotalMilliseconds : null,
             ms => ms.HasValue ? (TimeSpan?)TimeSpan.FromMilliseconds(ms.Value) : null);

        b.Ignore(m => m.ReceptionThroughputMbps);
        b.Ignore(m => m.TransferThroughputMbps);
        b.Ignore(m => m.SuccessRate);

        b.HasIndex(m => m.StudyInstanceUid)
         .IsUnique()
         .HasDatabaseName(IndexNames.MetricsStudy);

        b.HasIndex(m => m.FirstImageAt)
         .HasDatabaseName("ix_metrics_first_image_at");
    }
}
