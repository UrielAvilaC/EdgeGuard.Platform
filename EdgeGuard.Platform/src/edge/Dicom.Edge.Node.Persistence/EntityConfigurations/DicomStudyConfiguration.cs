namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class DicomStudyConfiguration : IEntityTypeConfiguration<DicomStudy>
{
    public void Configure(EntityTypeBuilder<DicomStudy> b)
    {
        b.ToTable(TableNames.DicomStudies);
        b.HasKey(s => s.StudyInstanceUid);

        b.Property(s => s.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(s => s.PatientId).HasColumnName("patient_id").HasMaxLength(64);
        b.Property(s => s.PatientName).HasColumnName("patient_name").HasMaxLength(256);
        b.Property(s => s.StudyDate).HasColumnName("study_date");
        b.Property(s => s.StudyDescription).HasColumnName("study_description").HasMaxLength(256);
        b.Property(s => s.AccessionNumber).HasColumnName("accession_number").HasMaxLength(64);
        b.Property(s => s.ReferringPhysician).HasColumnName("referring_physician").HasMaxLength(256);
        b.Property(s => s.Status).HasColumnName("status").HasConversion<int>();
        b.Property(s => s.InstanceCount).HasColumnName("instance_count").HasDefaultValue(0);
        b.Property(s => s.TotalSizeBytes).HasColumnName("total_size_bytes").HasDefaultValue(0L);
        b.Property(s => s.SourceAeTitle).HasColumnName("source_ae_title").HasMaxLength(16);
        b.Property(s => s.EdgeNodeId).HasColumnName("edge_node_id").HasMaxLength(64);
        b.Property(s => s.ReceivedAt).HasColumnName("received_at").IsRequired();
        b.Property(s => s.LastImageReceivedAt).HasColumnName("last_image_at");
        b.Property(s => s.SentToHubAt).HasColumnName("sent_to_hub_at");
        b.Property(s => s.ArchivedAt).HasColumnName("archived_at");
        b.Property(s => s.ErrorMessage).HasColumnName("error_message");
        b.Property(s => s.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        b.Property(s => s.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        b.Property(s => s.DeletedAt).HasColumnName("deleted_at");

        // Shadow properties
        b.Property<string?>("correlation_id").HasColumnName("correlation_id").HasMaxLength(64);
        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");
        b.Property<DateTime>("updated_at").HasDefaultValueSql("datetime('now')");

        b.Ignore(s => s.SeriesCount);
        b.Ignore(s => s.IsSentToHub);
        b.Ignore(s => s.IsArchived);
        b.Ignore(s => s.SizeMB);
        b.Ignore(s => s.ReceptionDuration);

        // FK → dicom_patients (restrict: keep patient if study deleted)
        b.HasOne(s => s.Patient)
         .WithMany()
         .HasForeignKey(s => s.PatientId)
         .OnDelete(DeleteBehavior.Restrict)
         .IsRequired(false);

        // FK → dicom_series (cascade: delete series when study deleted)
        b.HasMany(s => s.Series)
         .WithOne()
         .HasForeignKey(sr => sr.StudyInstanceUid)
         .OnDelete(DeleteBehavior.Cascade);

        // ── Indexes ──────────────────────────────────────────────────────────
        b.HasIndex(s => new { s.Status, s.ReceivedAt })
         .HasDatabaseName(IndexNames.StudiesStatusReceived);

        b.HasIndex(s => s.PatientId)
         .HasDatabaseName(IndexNames.StudiesPatient);

        b.HasIndex(s => new { s.SourceAeTitle, s.Status })
         .HasDatabaseName(IndexNames.StudiesSourceStatus);

        b.HasIndex(s => s.AccessionNumber)
         .HasDatabaseName(IndexNames.StudiesAccession)
         .HasFilter("\"accession_number\" IS NOT NULL");

        b.HasIndex(s => s.StudyDate)
         .HasDatabaseName(IndexNames.StudiesDate);

        // Partial: studies that haven't been sent yet
        b.HasIndex(s => s.Status)
         .HasDatabaseName(IndexNames.StudiesNotSent)
         .HasFilter("\"sent_to_hub_at\" IS NULL AND \"is_deleted\" = 0");

        // Composite for cleanup service queries
        b.HasIndex(s => new { s.IsDeleted, s.Status, s.ReceivedAt })
         .HasDatabaseName(IndexNames.StudiesCleanup);

        // Partial: correlation tracing
        b.HasIndex("correlation_id")
         .HasDatabaseName(IndexNames.StudiesCorrelation)
         .HasFilter("\"correlation_id\" IS NOT NULL");
    }
}
