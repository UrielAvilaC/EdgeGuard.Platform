using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class HubAuditLogConfiguration : IEntityTypeConfiguration<HubAuditLog>
{
    public void Configure(EntityTypeBuilder<HubAuditLog> builder)
    {
        builder.ToTable("hub_audit_logs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(50);
        builder.Property(a => a.EventType).HasConversion<string>().IsRequired().HasMaxLength(64);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(256);
        builder.Property(a => a.Severity).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(a => a.UserId).HasMaxLength(128);
        builder.Property(a => a.UserName).HasMaxLength(256);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.CorrelationId).HasMaxLength(64);
        builder.Property(a => a.EntityId).HasMaxLength(128);
        builder.Property(a => a.EntityType).HasMaxLength(128);
        builder.Property(a => a.ErrorMessage).HasMaxLength(2048);
        builder.Property(a => a.Details).HasColumnType("text");

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.EventType, a.CreatedAt });
        builder.HasIndex(a => a.CorrelationId)
               .HasFilter("correlation_id IS NOT NULL");
        builder.HasIndex(a => new { a.EntityType, a.EntityId })
               .HasFilter("entity_id IS NOT NULL");
    }
}
