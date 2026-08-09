using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class OutboxTopicConfiguration : IEntityTypeConfiguration<OutboxTopic>
{
    public void Configure(EntityTypeBuilder<OutboxTopic> builder)
    {
        builder.ToTable("outbox_topics");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasMaxLength(64);          // natural topic key, e.g. "node.config"
        builder.Property(t => t.Category).IsRequired().HasMaxLength(32);
        builder.Property(t => t.DisplayName).IsRequired().HasMaxLength(128);
        builder.Property(t => t.Description).HasMaxLength(512);
        builder.Property(t => t.Enabled).IsRequired();
        builder.Property(t => t.DefaultMaxAttempts).IsRequired();

        builder.HasIndex(t => t.Category).HasDatabaseName("ix_outbox_topics_category");
    }
}
