using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(50);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(128);
        builder.Property(t => t.Channel).HasConversion<string>().IsRequired().HasMaxLength(16);
        builder.Property(t => t.Format).HasConversion<string>().IsRequired().HasMaxLength(16);
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(512);
        builder.Property(t => t.Body).HasColumnType("text");

        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.IsActive);
    }
}
