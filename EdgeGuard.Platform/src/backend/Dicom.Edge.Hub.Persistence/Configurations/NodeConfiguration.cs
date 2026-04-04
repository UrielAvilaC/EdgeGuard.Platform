using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class NodeConfiguration : IEntityTypeConfiguration<Node>
{
    public void Configure(EntityTypeBuilder<Node> builder)
    {
        builder.ToTable("nodes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasMaxLength(50);
        builder.Property(n => n.Name).IsRequired().HasMaxLength(128);
        builder.Property(n => n.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(n => n.ApiEndpoint).HasMaxLength(512);
        builder.Property(n => n.Location).HasMaxLength(256);
        builder.Property(n => n.FacilityName).HasMaxLength(256);
        builder.Property(n => n.TimeZone).HasMaxLength(64);
        builder.Property(n => n.Version).HasMaxLength(32);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(32);

        builder.OwnsOne(n => n.AeTitle, vo =>
        {
            vo.Property(v => v.Value)
              .HasColumnName("ae_title")
              .IsRequired()
              .HasMaxLength(16);

            vo.HasIndex(v => v.Value).IsUnique();
        });

        builder.HasMany(n => n.PacsAssignments)
               .WithOne()
               .HasForeignKey(a => a.NodeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(n => n.PacsAssignments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.IsEnabled);

        builder.Ignore(n => n.DomainEvents);
    }
}
