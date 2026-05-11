namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable(TableNames.AuditLogs);
        b.HasKey(a => a.Id);

        b.Property(a => a.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(a => a.EventType).HasColumnName("event_type").HasConversion<int>();
        b.Property(a => a.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64);
        b.Property(a => a.SourceAeTitle).HasColumnName("source_ae_title").HasMaxLength(16);
        b.Property(a => a.DestinationAeTitle).HasColumnName("destination_ae_title").HasMaxLength(16);
        b.Property(a => a.Action).HasColumnName("action").HasMaxLength(128).IsRequired();
        b.Property(a => a.Timestamp).HasColumnName("timestamp").IsRequired();
        b.Property(a => a.UserId).HasColumnName("user_id").HasMaxLength(128);
        b.Property(a => a.UserName).HasColumnName("user_name").HasMaxLength(128);
        b.Property(a => a.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        b.Property(a => a.EdgeNodeId).HasColumnName("edge_node_id").HasMaxLength(64);
        b.Property(a => a.PatientId).HasColumnName("patient_id").HasMaxLength(64);
        b.Property(a => a.IsSuccess).HasColumnName("is_success").HasDefaultValue(true);
        b.Property(a => a.ErrorMessage).HasColumnName("error_message");
        b.Property(a => a.Details).HasColumnName("details");
        b.Property(a => a.Severity).HasColumnName("severity").HasDefaultValue(0);

        // Shadow property for correlation tracing
        b.Property<string?>("correlation_id").HasColumnName("correlation_id").HasMaxLength(64);

        b.HasIndex(a => a.Timestamp).HasDatabaseName(IndexNames.AuditTimestamp);
        b.HasIndex(a => new { a.EventType, a.Timestamp }).HasDatabaseName(IndexNames.AuditEventTime);

        b.HasIndex(a => a.StudyInstanceUid)
         .HasDatabaseName(IndexNames.AuditStudy)
         .HasFilter("\"study_instance_uid\" IS NOT NULL");

        b.HasIndex("correlation_id")
         .HasDatabaseName(IndexNames.AuditCorrelation)
         .HasFilter("\"correlation_id\" IS NOT NULL");

        b.HasIndex(a => a.PatientId)
         .HasDatabaseName(IndexNames.AuditPatient)
         .HasFilter("\"patient_id\" IS NOT NULL");
    }
}
