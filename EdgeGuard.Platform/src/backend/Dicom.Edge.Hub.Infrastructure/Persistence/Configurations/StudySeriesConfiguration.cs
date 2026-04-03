using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Configurations;

public class StudySeriesConfiguration : IEntityTypeConfiguration<StudySeries>
{
    public void Configure(EntityTypeBuilder<StudySeries> builder)
    {
        builder.ToTable("StudySeries");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasMaxLength(50);
        builder.Property(s => s.StudyId).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SeriesInstanceUid).IsRequired().HasMaxLength(64);
        builder.Property(s => s.Modality).HasMaxLength(16);
        builder.Property(s => s.SeriesDescription).HasMaxLength(512);

        builder.HasIndex(s => s.SeriesInstanceUid);
        builder.HasIndex(s => s.StudyId);
    }
}
