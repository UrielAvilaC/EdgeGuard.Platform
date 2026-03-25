namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class NodeSettingConfiguration : IEntityTypeConfiguration<NodeSettingEntity>
{
    public void Configure(EntityTypeBuilder<NodeSettingEntity> b)
    {
        b.ToTable(TableNames.NodeSettings);
        b.HasKey(s => s.Key);

        b.Property(s => s.Key).HasColumnName("key").HasMaxLength(128).IsRequired();
        b.Property(s => s.Value).HasColumnName("value").IsRequired();
        b.Property(s => s.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
        b.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
        b.Property(s => s.Description).HasColumnName("description");
        b.Property(s => s.ValueType).HasColumnName("value_type").HasMaxLength(16).IsRequired();
        b.Property(s => s.IsReadOnly).HasColumnName("is_read_only").HasDefaultValue(false);
        b.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(s => s.UpdatedAt).HasColumnName("updated_at").IsRequired();

        b.HasIndex(s => s.Category).HasDatabaseName("ix_node_settings_category");
    }
}
