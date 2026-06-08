namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class ModalityCatalogConfiguration : IEntityTypeConfiguration<ModalityCatalogEntry>
{
    public void Configure(EntityTypeBuilder<ModalityCatalogEntry> b)
    {
        b.ToTable(TableNames.Modalities);
        b.HasKey(m => m.Code);

        b.Property(m => m.Code).HasColumnName("code").HasMaxLength(16).IsRequired();
        b.Property(m => m.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        b.Property(m => m.IsSupported).HasColumnName("is_supported");
        b.Property(m => m.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(m => m.SortOrder).HasColumnName("sort_order");
    }
}
