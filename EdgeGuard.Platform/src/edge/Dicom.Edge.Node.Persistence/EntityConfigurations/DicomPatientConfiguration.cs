namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class DicomPatientConfiguration : IEntityTypeConfiguration<DicomPatient>
{
    public void Configure(EntityTypeBuilder<DicomPatient> b)
    {
        b.ToTable(TableNames.DicomPatients);
        b.HasKey(p => p.PatientId);

        b.Property(p => p.PatientId).HasColumnName("patient_id").HasMaxLength(64).IsRequired();
        b.Property(p => p.PatientName).HasColumnName("patient_name").HasMaxLength(256).IsRequired();
        b.Property(p => p.BirthDate).HasColumnName("birth_date");
        b.Property(p => p.Sex).HasColumnName("sex").HasMaxLength(1);
        b.Property(p => p.PatientAge).HasColumnName("patient_age").HasMaxLength(8);
        b.Property(p => p.PatientWeightKg).HasColumnName("patient_weight_kg");
        b.Property(p => p.PatientHeightM).HasColumnName("patient_height_m");
        b.Property(p => p.AccessionNumber).HasColumnName("accession_number").HasMaxLength(64);
        b.Property(p => p.ReferringPhysician).HasColumnName("referring_physician").HasMaxLength(256);
        b.Property(p => p.InstitutionName).HasColumnName("institution_name").HasMaxLength(128);
        b.Property(p => p.MedicalRecordNumber).HasColumnName("medical_record_number").HasMaxLength(64);
        b.Property(p => p.Allergies).HasColumnName("allergies");
        b.Property(p => p.Comments).HasColumnName("comments");

        b.Ignore(p => p.AgeYears);
        b.Ignore(p => p.DisplayName);

        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");

        b.HasIndex(p => p.PatientName).HasDatabaseName("ix_patient_name");
    }
}
