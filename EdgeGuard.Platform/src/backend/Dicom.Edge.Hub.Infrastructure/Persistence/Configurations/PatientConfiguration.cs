using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(50);
        builder.Property(p => p.PatientName).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Sex).HasMaxLength(16);
        builder.Property(p => p.IssuerOfPatientId).HasMaxLength(128);
        builder.Property(p => p.OtherPatientIds).HasMaxLength(512);
        builder.Property(p => p.FacilitySource).HasMaxLength(128);
        builder.Property(p => p.CreatedByNodeId).HasMaxLength(50);

        builder.OwnsOne(p => p.PatientDicomId, vo =>
        {
            vo.Property(v => v.Value)
              .HasColumnName("PatientDicomId")
              .IsRequired()
              .HasMaxLength(64);

            vo.HasIndex(v => v.Value);
        });

        builder.HasIndex(p => p.PatientName);
        builder.HasIndex(p => p.IsActive);

        builder.Ignore(p => p.DomainEvents);
    }
}
