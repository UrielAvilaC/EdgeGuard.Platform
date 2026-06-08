using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NodeOutboxMessageConfiguration : IEntityTypeConfiguration<NodeOutboxMessage>
{
    public void Configure(EntityTypeBuilder<NodeOutboxMessage> builder)
    {
        builder.ToTable("node_outbox_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id).HasMaxLength(50);
        builder.Property(m => m.TopicId).IsRequired().HasMaxLength(64);
        builder.Property(m => m.NodeId).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(m => m.LastError).HasMaxLength(2048);

        builder.HasOne<OutboxTopic>()
               .WithMany()
               .HasForeignKey(m => m.TopicId)
               .OnDelete(DeleteBehavior.Restrict);

        // Drain: due pending rows first.
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt })
               .HasDatabaseName("ix_node_outbox_messages_status_next_attempt");
        // Coalescing: pending rows per node + topic.
        builder.HasIndex(m => new { m.NodeId, m.TopicId, m.Status })
               .HasDatabaseName("ix_node_outbox_messages_node_topic_status");
        builder.HasIndex(m => m.CreatedAt);
    }
}
