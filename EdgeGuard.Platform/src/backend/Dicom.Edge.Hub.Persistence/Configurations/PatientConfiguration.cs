using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(50);
        builder.Property(p => p.PatientName).IsRequired().HasMaxLength(256);
        builder.Property(p => p.BirthDate)
       .HasMaxLength(8)
       .HasConversion(new ValueConverter<DateOnly?, string?>(
           v => v.HasValue ? v.Value.ToString("yyyyMMdd") : null,
           v => !string.IsNullOrEmpty(v) ? DateOnly.ParseExact(v, "yyyyMMdd") : null));
        builder.Property(p => p.Sex).HasMaxLength(16);
        builder.Property(p => p.PhoneNumber).HasMaxLength(32);
        builder.Property(p => p.Email).HasMaxLength(256);
        builder.Property(p => p.IssuerOfPatientId).HasMaxLength(128);
        builder.Property(p => p.OtherPatientIds).HasMaxLength(512);
        builder.Property(p => p.FacilitySource).HasMaxLength(128);
        builder.Property(p => p.CreatedByNodeId).HasMaxLength(50);
        builder.Property(p => p.MergedIntoPatientId).HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");

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
        builder.HasIndex(p => p.MergedIntoPatientId)
               .HasFilter("merged_into_patient_id IS NOT NULL")
               .HasDatabaseName("ix_patients_merged_into");

        builder.Ignore(p => p.IsMerged);
        builder.Ignore(p => p.DomainEvents);
    }
}
