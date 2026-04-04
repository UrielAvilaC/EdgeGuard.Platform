using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Configurations;

public class HealthCheckRecordConfiguration : IEntityTypeConfiguration<HealthCheckRecord>
{
    public void Configure(EntityTypeBuilder<HealthCheckRecord> builder)
    {
        builder.ToTable("HealthCheckRecords");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasMaxLength(50);
        builder.Property(h => h.NodeId).IsRequired().HasMaxLength(50);
        builder.Property(h => h.ReportedNodeStatus).HasConversion<string>().HasMaxLength(32);

        builder.HasMany(h => h.PacsResults)
               .WithOne()
               .HasForeignKey(r => r.HealthCheckRecordId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(h => h.PacsResults).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(h => h.NodeId);
        builder.HasIndex(h => h.ReceivedAt);

        builder.Ignore(h => h.DomainEvents);
    }
}
