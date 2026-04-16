using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class WhatsAppTemplateConfiguration : IEntityTypeConfiguration<WhatsAppTemplate>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplate> builder)
    {
        builder.ToTable("whatsapp_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(50);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(128);
        builder.Property(t => t.ContentSid).IsRequired().HasMaxLength(64);
        builder.Property(t => t.Description).HasMaxLength(512);
        builder.Property(t => t.IsActive).IsRequired();

        builder.HasMany(t => t.Variables)
               .WithOne()
               .HasForeignKey(v => v.TemplateId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Name).IsUnique();

        builder.Ignore(t => t.DomainEvents);
    }
}
