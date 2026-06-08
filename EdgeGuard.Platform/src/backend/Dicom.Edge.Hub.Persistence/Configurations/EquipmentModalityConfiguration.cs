using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Dicom.Edge.Hub.Domain.Aggregates.Modalities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class EquipmentModalityConfiguration : IEntityTypeConfiguration<EquipmentModality>
{
    public void Configure(EntityTypeBuilder<EquipmentModality> b)
    {
        b.ToTable("equipment_modalities");
        b.HasKey(m => new { m.EquipmentId, m.ModalityCode });

        b.Property(m => m.EquipmentId).HasMaxLength(100).IsRequired();
        b.Property(m => m.ModalityCode).HasMaxLength(16).IsRequired();

        // FK to the modality catalog (principal key = Modality.Code). Restrict so a
        // catalog code that is still assigned cannot be deleted out from under equipment.
        b.HasOne<Modality>()
         .WithMany()
         .HasForeignKey(m => m.ModalityCode)
         .HasPrincipalKey(x => x.Code)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(m => m.ModalityCode).HasDatabaseName("ix_equipment_modalities_code");
    }
}
