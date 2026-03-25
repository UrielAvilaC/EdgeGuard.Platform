namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class DicomInstanceConfiguration : IEntityTypeConfiguration<DicomInstance>
{
    public void Configure(EntityTypeBuilder<DicomInstance> b)
    {
        b.ToTable(TableNames.DicomInstances);
        b.HasKey(i => i.SopInstanceUid);

        b.Property(i => i.SopInstanceUid).HasColumnName("sop_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(i => i.SeriesInstanceUid).HasColumnName("series_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(i => i.SopClassUid).HasColumnName("sop_class_uid").HasMaxLength(64).IsRequired();
        b.Property(i => i.InstanceNumber).HasColumnName("instance_number");
        b.Property(i => i.FilePath).HasColumnName("file_path").IsRequired();

        // Shadow properties for fields useful in storage management but not in model
        b.Property<long>("file_size_bytes").HasColumnName("file_size_bytes").HasDefaultValue(0L);
        b.Property<string?>("transfer_syntax_uid").HasColumnName("transfer_syntax_uid").HasMaxLength(64);
        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");

        b.HasIndex(i => i.SeriesInstanceUid).HasDatabaseName(IndexNames.InstanceSeries);
        b.HasIndex(i => i.FilePath).IsUnique().HasDatabaseName(IndexNames.InstanceFilePath);
    }
}
