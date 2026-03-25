namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class StudyArchiveConfiguration : IEntityTypeConfiguration<StudyArchive>
{
    public void Configure(EntityTypeBuilder<StudyArchive> b)
    {
        b.ToTable(TableNames.StudyArchives);
        b.HasKey(a => a.Id);

        b.Property(a => a.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(a => a.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(a => a.ArchivedAt).HasColumnName("archived_at").IsRequired();
        b.Property(a => a.ArchiveLocation).HasColumnName("archive_location").IsRequired();
        b.Property(a => a.SizeBytes).HasColumnName("size_bytes").HasDefaultValue(0L);
        b.Property(a => a.DeleteScheduledAt).HasColumnName("delete_scheduled_at");
        b.Property(a => a.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.Property(a => a.DeletedAt).HasColumnName("deleted_at");
        b.Property(a => a.Reason).HasColumnName("reason").HasConversion<int>();

        b.HasIndex(a => a.StudyInstanceUid).HasDatabaseName(IndexNames.ArchiveStudy);

        // Partial: rows pending physical deletion
        b.HasIndex(a => a.DeleteScheduledAt)
         .HasDatabaseName(IndexNames.ArchivePending)
         .HasFilter("\"is_deleted\" = 0 AND \"delete_scheduled_at\" IS NOT NULL");
    }
}
