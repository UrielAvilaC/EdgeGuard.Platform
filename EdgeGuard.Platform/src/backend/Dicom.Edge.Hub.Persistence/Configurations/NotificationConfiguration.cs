using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasMaxLength(50);
        builder.Property(n => n.StudyId).IsRequired().HasMaxLength(50);
        builder.Property(n => n.PatientId).HasMaxLength(50);
        builder.Property(n => n.PhoneNumber).IsRequired().HasMaxLength(32);
        builder.Property(n => n.NormalizedPhone).HasMaxLength(32);
        builder.Property(n => n.TemplateId).HasMaxLength(50);
        builder.Property(n => n.ContentSid).HasMaxLength(64);
        builder.Property(n => n.ContentVariables).HasColumnType("text");
        builder.Property(n => n.StudyStatus).HasMaxLength(32);
        builder.Property(n => n.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(n => n.TriggeredBy).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(n => n.ProviderName).HasMaxLength(32);
        builder.Property(n => n.ProviderMessageId).HasMaxLength(128);
        builder.Property(n => n.LastError).HasMaxLength(2048);

        // Unified outbox columns. Migration: RenameWhatsAppNotificationToNotification.
        builder.Property(n => n.Channel).HasConversion<string>().IsRequired().HasMaxLength(16);

        // Outbox topic catalog FK (derived from Channel). Migration: AddNotificationTopicId.
        builder.Property(n => n.TopicId).IsRequired().HasMaxLength(64);
        builder.HasOne<OutboxTopic>()
               .WithMany()
               .HasForeignKey(n => n.TopicId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.Property(n => n.ToEmail).HasMaxLength(256);
        builder.Property(n => n.Subject).HasMaxLength(512);
        builder.Property(n => n.RenderedBody).HasColumnType("text");
        builder.Property(n => n.AttachmentPath).HasMaxLength(512);
        builder.Property(n => n.ImageLink).HasMaxLength(1024);

        builder.HasIndex(n => n.StudyId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.StudyStatus);
        builder.HasIndex(n => n.ProviderName);
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.Status, n.CreatedAt })
               .HasFilter("status = 'Pending'");
        // Outbox drain index (per-channel, due records first).
        builder.HasIndex(n => new { n.Channel, n.Status, n.NextAttemptAt });
    }
}
