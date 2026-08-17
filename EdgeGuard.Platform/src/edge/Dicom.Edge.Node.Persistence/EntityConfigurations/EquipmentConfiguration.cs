namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> b)
    {
        b.ToTable(TableNames.Equipment);
        b.HasKey(e => e.Id);

        b.Property(e => e.Id).HasColumnName("id").HasMaxLength(100).IsRequired();
        b.Property(e => e.AeTitle).HasColumnName("ae_title").HasMaxLength(16).IsRequired();
        b.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100);
        b.Property(e => e.StationAeTitle).HasColumnName("station_ae_title").HasMaxLength(16);
        b.Property(e => e.StationName).HasColumnName("station_name").HasMaxLength(100);
        b.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        b.Property(e => e.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => e.AeTitle).IsUnique().HasDatabaseName("uq_equipment_ae_title");

        b.HasMany(e => e.Modalities)
         .WithOne()
         .HasForeignKey(m => m.EquipmentId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
