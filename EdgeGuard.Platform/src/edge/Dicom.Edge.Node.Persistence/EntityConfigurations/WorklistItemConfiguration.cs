namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class WorklistItemConfiguration : IEntityTypeConfiguration<WorklistItem>
{
    public void Configure(EntityTypeBuilder<WorklistItem> b)
    {
        b.ToTable(TableNames.WorklistItems);
        b.HasKey(w => w.AccessionNumber);

        b.Property(w => w.AccessionNumber).HasColumnName("accession_number").HasMaxLength(64).IsRequired();
        b.Property(w => w.PatientId).HasColumnName("patient_id").HasMaxLength(64).IsRequired();
        b.Property(w => w.PatientName).HasColumnName("patient_name").HasMaxLength(256).IsRequired();
        b.Property(w => w.ScheduledDate).HasColumnName("scheduled_date").IsRequired();
        b.Property(w => w.Modality).HasColumnName("modality").HasMaxLength(16).IsRequired();
        b.Property(w => w.ProcedureDescription).HasColumnName("procedure_description").HasMaxLength(256).IsRequired();

        // ── MWL fields required by the C-FIND SCP for proper filtering and response ──
        b.Property(w => w.PatientBirthDate)
            .HasColumnName("patient_birth_date").HasMaxLength(8);
        b.Property(w => w.PatientSex)
            .HasColumnName("patient_sex").HasMaxLength(4);
        b.Property(w => w.ScheduledStationAeTitle)
            .HasColumnName("scheduled_station_ae_title").HasMaxLength(16);
        b.Property(w => w.ScheduledPerformingPhysicianName)
            .HasColumnName("scheduled_performing_physician_name").HasMaxLength(256);
        b.Property(w => w.ScheduledProcedureStepId)
            .HasColumnName("scheduled_procedure_step_id").HasMaxLength(64);
        b.Property(w => w.RequestedProcedureId)
            .HasColumnName("requested_procedure_id").HasMaxLength(64);
        b.Property(w => w.ReferringPhysicianName)
            .HasColumnName("referring_physician_name").HasMaxLength(256);
        b.Property(w => w.StudyInstanceUid)
            .HasColumnName("study_instance_uid").HasMaxLength(64);

        // Shadow properties
        b.Property<string>("status").HasColumnName("status").HasMaxLength(32).HasDefaultValue("pending");
        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");
        b.Property<DateTime>("updated_at").HasDefaultValueSql("datetime('now')");

        b.HasIndex(w => new { w.ScheduledDate, w.Modality }).HasDatabaseName(IndexNames.WorklistScheduled);
        b.HasIndex(w => w.PatientId).HasDatabaseName(IndexNames.WorklistPatient);
        b.HasIndex("status").HasDatabaseName(IndexNames.WorklistStatus);

        // P0-MWL: index for filtering by scheduled station AE (modality filter)
        b.HasIndex(w => w.ScheduledStationAeTitle).HasDatabaseName("ix_worklist_items_station_ae");
    }
}
