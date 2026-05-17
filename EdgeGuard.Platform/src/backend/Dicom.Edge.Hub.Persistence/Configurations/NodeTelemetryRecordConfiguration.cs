using Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NodeTelemetryRecordConfiguration : IEntityTypeConfiguration<NodeTelemetryRecord>
{
    public void Configure(EntityTypeBuilder<NodeTelemetryRecord> b)
    {
        b.ToTable("node_telemetry_records");
        b.HasKey(r => r.Id);

        b.Property(r => r.Id).HasMaxLength(50);
        b.Property(r => r.NodeId).IsRequired().HasMaxLength(50);
        b.Property(r => r.ReportedAt).IsRequired();
        b.Property(r => r.PeriodStart).IsRequired();
        b.Property(r => r.PeriodEnd).IsRequired();

        b.Property(r => r.TotalAssociations).HasDefaultValue(0);
        b.Property(r => r.AcceptedAssociations).HasDefaultValue(0);
        b.Property(r => r.RejectedAssociations).HasDefaultValue(0);
        b.Property(r => r.AbortedAssociations).HasDefaultValue(0);
        b.Property(r => r.TotalImagesReceived).HasDefaultValue(0);
        b.Property(r => r.CompletedStudies).HasDefaultValue(0);
        b.Property(r => r.TotalBytesReceived).HasDefaultValue(0L);
        b.Property(r => r.AverageReceptionDurationMs);
        b.Property(r => r.AverageThroughputMbps);

        b.HasIndex(r => r.NodeId).HasDatabaseName("ix_node_telemetry_records_node_id");
        b.HasIndex(r => r.ReportedAt).HasDatabaseName("ix_node_telemetry_records_reported_at");

        b.Ignore(r => r.DomainEvents);
    }
}
