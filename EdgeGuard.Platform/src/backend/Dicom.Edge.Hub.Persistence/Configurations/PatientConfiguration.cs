using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
               .ValueGeneratedNever()
               .HasColumnName("patient_id")
               .HasMaxLength(50);

        builder.Property(p => p.PatientName)
               .IsRequired()
               .HasMaxLength(256)
               .HasColumnName("name");

        builder.Property(p => p.Sex)
               .HasMaxLength(16)
               .HasColumnName("sex");
        builder.Property(p => p.BirthDate)
               .HasColumnName("birth_date")
               ;

        builder.Property(p => p.PhoneNumber)
               .HasMaxLength(32)
               .HasColumnName("phone_number");

        builder.Property(p => p.Email)
               .HasMaxLength(256)
               .HasColumnName("email");
        builder.Property(p => p.IssuerOfPatientId)
                .HasMaxLength(128)
                .HasColumnName("issuer_of_patient_id");

        builder.Property(p => p.OtherPatientIds)
               .HasMaxLength(512)
               .HasColumnName("other_patient_ids");

        builder.Property(p => p.FacilitySource)
               .HasMaxLength(128)
               .HasColumnName("facility_source");

        builder.Property(p => p.IsActive)
               .HasColumnName("is_active")
               .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.CreatedByNodeId).HasMaxLength(50);

        builder.OwnsOne(p => p.PatientDicomId, vo =>
        {
            vo.Property(v => v.Value)
              .HasColumnName("patient_dicom_id")
              .IsRequired()
              .HasMaxLength(64);

            vo.HasIndex(v => v.Value);
        });

        builder.HasIndex(p => p.PatientName);
        builder.HasIndex(p => p.PhoneNumber);
        builder.HasIndex(p => p.IsActive);

        builder.Ignore(p => p.DomainEvents);
    }
}
