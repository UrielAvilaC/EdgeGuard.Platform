using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class StudyStatusAuditConfiguration : IEntityTypeConfiguration<StudyStatusAudit>
{
    public void Configure(EntityTypeBuilder<StudyStatusAudit> builder)
    {
        builder.ToTable("study_status_audits");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(50);
        builder.Property(a => a.StudyId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PreviousStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.NewStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(a => a.ChangedByNodeId).HasMaxLength(50);
        builder.Property(a => a.Reason).HasMaxLength(1024);

        builder.HasIndex(a => a.StudyId);
        builder.HasIndex(a => a.ChangedAt);
    }
}
