using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class PacsSendAuditConfiguration : IEntityTypeConfiguration<PacsSendAudit>
{
    public void Configure(EntityTypeBuilder<PacsSendAudit> builder)
    {
        builder.ToTable("pacs_send_audits");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(50);
        builder.Property(a => a.StudyId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PacsId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PacsAeTitle).HasMaxLength(16);
        builder.Property(a => a.ResponseCode).HasMaxLength(32);
        builder.Property(a => a.ErrorMessage).HasMaxLength(2048);

        builder.HasIndex(a => a.StudyId);
        builder.HasIndex(a => a.PacsId);
        builder.HasIndex(a => a.SentAt);
        builder.HasIndex(a => new { a.StudyId, a.Attempt });
    }
}
