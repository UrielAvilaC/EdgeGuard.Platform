using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class StudyConfiguration : IEntityTypeConfiguration<Study>
{
    public void Configure(EntityTypeBuilder<Study> builder)
    {
        builder.ToTable("studies");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasMaxLength(50);
        builder.Property(s => s.AccessionNumber).HasMaxLength(64);
        builder.Property(s => s.StudyDescription).HasMaxLength(512);
        builder.Property(s => s.ReferringPhysician).HasMaxLength(256);
        builder.Property(s => s.PatientId).HasMaxLength(50);
        builder.Property(s => s.PatientName).HasMaxLength(256);
        builder.Property(s => s.SourceNodeId).HasMaxLength(50);
        builder.Property(s => s.SourceAeTitle).HasMaxLength(16);
        builder.Property(s => s.ReceivingAeTitle).HasMaxLength(16);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.WorklistReadByModality).HasMaxLength(16);
        builder.Property(s => s.TargetPacsId).HasMaxLength(50);
        builder.Property(s => s.PacsSendLastError).HasMaxLength(1024);

        builder.OwnsOne(s => s.StudyInstanceUid, vo =>
        {
            vo.Property(v => v.Value)
              .HasColumnName("study_instance_uid")
              .IsRequired()
              .HasMaxLength(64);

            vo.HasIndex(v => v.Value).IsUnique();
        });

        builder.HasMany(s => s.Series)
               .WithOne()
               .HasForeignKey(ss => ss.StudyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.StatusAudits)
               .WithOne()
               .HasForeignKey(a => a.StudyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Series).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(s => s.StatusAudits).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.PatientId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.StudyDate);
        builder.HasIndex(s => s.SourceNodeId);

        builder.Ignore(s => s.DomainEvents);
    }
}
