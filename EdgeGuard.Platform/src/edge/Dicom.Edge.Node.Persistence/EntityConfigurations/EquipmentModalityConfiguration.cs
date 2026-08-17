namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class EquipmentModalityConfiguration : IEntityTypeConfiguration<EquipmentModality>
{
    public void Configure(EntityTypeBuilder<EquipmentModality> b)
    {
        b.ToTable(TableNames.EquipmentModalities);
        b.HasKey(m => new { m.EquipmentId, m.ModalityCode });

        b.Property(m => m.EquipmentId).HasColumnName("equipment_id").HasMaxLength(100).IsRequired();
        b.Property(m => m.ModalityCode).HasColumnName("modality_code").HasMaxLength(16).IsRequired();

        b.HasIndex(m => m.ModalityCode).HasDatabaseName("ix_equipment_modalities_code");
    }
}
