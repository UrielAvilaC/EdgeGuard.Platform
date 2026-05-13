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

        // Shadow properties
        b.Property<string>("status").HasColumnName("status").HasMaxLength(32).HasDefaultValue("pending");
        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");
        b.Property<DateTime>("updated_at").HasDefaultValueSql("datetime('now')");

        b.HasIndex(w => new { w.ScheduledDate, w.Modality }).HasDatabaseName(IndexNames.WorklistScheduled);
        b.HasIndex(w => w.PatientId).HasDatabaseName(IndexNames.WorklistPatient);
        b.HasIndex("status").HasDatabaseName(IndexNames.WorklistStatus);
    }
}
