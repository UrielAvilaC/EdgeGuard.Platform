using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NotificationAutoSendRuleConfiguration : IEntityTypeConfiguration<NotificationAutoSendRule>
{
    public void Configure(EntityTypeBuilder<NotificationAutoSendRule> builder)
    {
        builder.ToTable("notification_auto_send_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(50);
        builder.Property(r => r.StudyStatus).IsRequired().HasMaxLength(32);
        builder.Property(r => r.TemplateId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.IsEnabled).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(512);

        // Fase 7: multichannel auto-send. Migration: RenameAutoSendRuleAddChannel.
        builder.Property(r => r.Channel).IsRequired().HasMaxLength(16).HasDefaultValue("WhatsApp");
        builder.Property(r => r.AttachPdf).IsRequired();
        builder.Property(r => r.IncludeQr).IsRequired();

        // One rule per (status, channel).
        builder.HasIndex(r => new { r.StudyStatus, r.Channel }).IsUnique();
    }
}
