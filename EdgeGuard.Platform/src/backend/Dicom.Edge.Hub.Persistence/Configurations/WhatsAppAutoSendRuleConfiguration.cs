using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class WhatsAppAutoSendRuleConfiguration : IEntityTypeConfiguration<WhatsAppAutoSendRule>
{
    public void Configure(EntityTypeBuilder<WhatsAppAutoSendRule> builder)
    {
        builder.ToTable("whatsapp_auto_send_rules");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(50);
        builder.Property(r => r.StudyStatus).IsRequired().HasMaxLength(32);
        builder.Property(r => r.TemplateId).IsRequired().HasMaxLength(50);
        builder.Property(r => r.IsEnabled).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(512);

        builder.HasIndex(r => r.StudyStatus).IsUnique();
    }
}
