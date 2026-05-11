namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class DicomSeriesConfiguration : IEntityTypeConfiguration<DicomSeries>
{
    public void Configure(EntityTypeBuilder<DicomSeries> b)
    {
        b.ToTable(TableNames.DicomSeries);
        b.HasKey(s => s.SeriesInstanceUid);

        b.Property(s => s.SeriesInstanceUid).HasColumnName("series_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(s => s.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(s => s.Modality).HasColumnName("modality").HasMaxLength(16).IsRequired();
        b.Property(s => s.InstanceCount).HasColumnName("instance_count").HasDefaultValue(0);
        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");

        // FK to instances is configured in DicomInstanceConfiguration
        b.HasMany(s => s.Instances)
         .WithOne()
         .HasForeignKey(i => i.SeriesInstanceUid)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(s => s.StudyInstanceUid).HasDatabaseName(IndexNames.SeriesStudy);
        b.HasIndex(s => s.Modality).HasDatabaseName(IndexNames.SeriesModality);
    }
}
