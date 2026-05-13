using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class WhatsAppTemplateVariableConfiguration : IEntityTypeConfiguration<WhatsAppTemplateVariable>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplateVariable> builder)
    {
        builder.ToTable("whatsapp_template_variables");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasMaxLength(50);
        builder.Property(v => v.TemplateId).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Position).IsRequired();
        builder.Property(v => v.Tag).IsRequired().HasMaxLength(64);

        builder.HasIndex(v => new { v.TemplateId, v.Position }).IsUnique();
    }
}
