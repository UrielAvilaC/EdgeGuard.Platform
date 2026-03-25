namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class EdgeQueueItemConfiguration : IEntityTypeConfiguration<EdgeQueueItem>
{
    public void Configure(EntityTypeBuilder<EdgeQueueItem> b)
    {
        b.ToTable(TableNames.EdgeQueueItems);
        b.HasKey(q => q.Id);

        b.Property(q => q.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(q => q.StudyInstanceUid).HasColumnName("study_instance_uid").HasMaxLength(64).IsRequired();
        b.Property(q => q.Status).HasColumnName("status").HasConversion<int>();
        b.Property(q => q.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
        b.Property(q => q.Priority).HasColumnName("priority").HasDefaultValue(5);
        b.Property(q => q.Destination).HasColumnName("destination").HasMaxLength(512).IsRequired();
        b.Property(q => q.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(q => q.LastAttempt).HasColumnName("last_attempt_at");
        b.Property(q => q.NextRetryAt).HasColumnName("next_retry_at");
        b.Property(q => q.ErrorMessage).HasColumnName("error_message");
        b.Property(q => q.SizeBytes).HasColumnName("size_bytes").HasDefaultValue(0L);
        b.Property(q => q.InstanceCount).HasColumnName("instance_count").HasDefaultValue(0);
        b.Property(q => q.IsLocked).HasColumnName("is_locked").HasDefaultValue(false);
        b.Property(q => q.LockedAt).HasColumnName("locked_at");
        b.Property(q => q.LockedBy).HasColumnName("locked_by").HasMaxLength(64);
        b.Property(q => q.Metadata).HasColumnName("context_json");

        b.Ignore(q => q.IsEligibleForRetry);
        b.Ignore(q => q.Age);

        // HOT PATH index: used by the queue worker to dequeue next item
        b.HasIndex(q => new { q.Status, q.Priority, q.NextRetryAt })
         .HasDatabaseName(IndexNames.QueueDequeue);

        b.HasIndex(q => q.StudyInstanceUid)
         .HasDatabaseName(IndexNames.QueueStudy);

        // Partial: detect stale locks (worker crashed without releasing)
        b.HasIndex(q => new { q.IsLocked, q.LockedAt })
         .HasDatabaseName(IndexNames.QueueStaleLock)
         .HasFilter("\"is_locked\" = 1");
    }
}
