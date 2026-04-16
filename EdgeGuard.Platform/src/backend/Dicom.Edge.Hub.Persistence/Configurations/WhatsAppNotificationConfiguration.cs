using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class WhatsAppNotificationConfiguration : IEntityTypeConfiguration<WhatsAppNotification>
{
    public void Configure(EntityTypeBuilder<WhatsAppNotification> builder)
    {
        builder.ToTable("whatsapp_notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasMaxLength(50);
        builder.Property(n => n.StudyId).IsRequired().HasMaxLength(50);
        builder.Property(n => n.PatientId).HasMaxLength(50);
        builder.Property(n => n.PhoneNumber).IsRequired().HasMaxLength(32);
        builder.Property(n => n.NormalizedPhone).HasMaxLength(32);
        builder.Property(n => n.TemplateId).HasMaxLength(50);
        builder.Property(n => n.ContentSid).HasMaxLength(64);
        builder.Property(n => n.StudyStatus).HasMaxLength(32);
        builder.Property(n => n.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(n => n.TriggeredBy).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(n => n.ProviderName).HasMaxLength(32);
        builder.Property(n => n.ProviderMessageId).HasMaxLength(128);
        builder.Property(n => n.LastError).HasMaxLength(2048);

        builder.HasIndex(n => n.StudyId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.StudyStatus);
        builder.HasIndex(n => n.ProviderName);
        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => new { n.Status, n.CreatedAt })
               .HasFilter("status = 'Pending'");
    }
}
