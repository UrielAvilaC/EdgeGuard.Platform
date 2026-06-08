using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class NodeEquipmentConfiguration : IEntityTypeConfiguration<NodeEquipment>
{
    public void Configure(EntityTypeBuilder<NodeEquipment> b)
    {
        b.ToTable("node_equipment");
        b.HasKey(e => e.Id);

        b.Property(e => e.Id).HasMaxLength(100).IsRequired();
        b.Property(e => e.NodeId).HasMaxLength(100).IsRequired();
        b.Property(e => e.AeTitle).HasMaxLength(16).IsRequired();
        b.Property(e => e.DisplayName).HasMaxLength(100);
        b.Property(e => e.StationAeTitle).HasMaxLength(16);
        b.Property(e => e.StationName).HasMaxLength(100);
        b.Property(e => e.IpAddress).HasMaxLength(45);
        b.Property(e => e.Location).HasMaxLength(200);
        b.Property(e => e.Department).HasMaxLength(100);
        b.Property(e => e.Manufacturer).HasMaxLength(100);
        b.Property(e => e.Model).HasMaxLength(100);
        b.Property(e => e.Notes).HasMaxLength(1000);

        // One equipment per (node, AE title).
        b.HasIndex(e => new { e.NodeId, e.AeTitle })
         .IsUnique()
         .HasDatabaseName("ix_node_equipment_node_ae");

        b.HasMany(e => e.Modalities)
         .WithOne()
         .HasForeignKey(m => m.EquipmentId)
         .OnDelete(DeleteBehavior.Cascade);

        // Backing field access for the encapsulated modality collection.
        b.Navigation(e => e.Modalities)
         .HasField("_modalities")
         .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Ignore(e => e.ModalityCodes);
        b.Ignore(e => e.DomainEvents);
    }
}
