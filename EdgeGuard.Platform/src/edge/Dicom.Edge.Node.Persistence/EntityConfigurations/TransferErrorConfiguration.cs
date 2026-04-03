namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class TransferErrorConfiguration : IEntityTypeConfiguration<TransferError>
{
    public void Configure(EntityTypeBuilder<TransferError> b)
    {
        b.ToTable(TableNames.TransferErrors);
        b.HasKey(e => e.Id);

        b.Property(e => e.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(e => e.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64);
        b.Property(e => e.ErrorType).HasColumnName("error_type").HasConversion<int>();
        b.Property(e => e.ErrorMessage).HasColumnName("error_message").IsRequired();
        b.Property(e => e.StackTrace).HasColumnName("stack_trace");
        b.Property(e => e.OccurredAt).HasColumnName("occurred_at").IsRequired();
        b.Property(e => e.RetryAttempt).HasColumnName("retry_attempt").HasDefaultValue(0);
        b.Property(e => e.IsResolved).HasColumnName("is_resolved").HasDefaultValue(false);
        b.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
        b.Property(e => e.ResolutionNotes).HasColumnName("resolution_notes");
        b.Property(e => e.EdgeNodeId).HasColumnName("edge_node_id").HasMaxLength(64);
        b.Property(e => e.AdditionalContext).HasColumnName("context_json");

        b.Ignore(e => e.IsRetryable);

        b.HasIndex(e => e.StudyInstanceUid).HasDatabaseName(IndexNames.ErrorsStudy);
        b.HasIndex(e => e.OccurredAt).HasDatabaseName(IndexNames.ErrorsOccurred);

        // Partial: active unresolved errors only
        b.HasIndex(e => new { e.ErrorType, e.IsResolved })
         .HasDatabaseName(IndexNames.ErrorsUnresolved)
         .HasFilter("\"is_resolved\" = 0");
    }
}
