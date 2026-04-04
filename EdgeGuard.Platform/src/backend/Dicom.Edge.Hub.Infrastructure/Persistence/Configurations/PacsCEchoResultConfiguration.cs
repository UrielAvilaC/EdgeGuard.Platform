using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Configurations;

public class PacsCEchoResultConfiguration : IEntityTypeConfiguration<PacsCEchoResult>
{
    public void Configure(EntityTypeBuilder<PacsCEchoResult> builder)
    {
        builder.ToTable("PacsCEchoResults");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(50);
        builder.Property(r => r.HealthCheckRecordId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.PacsId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.PacsAeTitle).HasMaxLength(16);
        builder.Property(r => r.ErrorMessage).HasMaxLength(1024);

        builder.HasIndex(r => r.PacsId);
        builder.HasIndex(r => r.CheckedAt);
    }
}
