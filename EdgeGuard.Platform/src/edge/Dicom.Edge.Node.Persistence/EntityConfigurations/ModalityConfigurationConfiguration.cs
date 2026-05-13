namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class ModalityConfigurationConfiguration : IEntityTypeConfiguration<ModalityConfiguration>
{
    public void Configure(EntityTypeBuilder<ModalityConfiguration> b)
    {
        b.ToTable(TableNames.ModalityConfigurations);
        b.HasKey(m => m.Id);

        b.Property(m => m.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(m => m.AETitle).HasColumnName("ae_title").HasMaxLength(16).IsRequired();
        b.Property(m => m.DisplayName).HasColumnName("display_name").HasMaxLength(100);
        b.Property(m => m.IpAddress).HasColumnName("ip_address").HasMaxLength(64).IsRequired();
        b.Property(m => m.Port).HasColumnName("port").HasDefaultValue(104);
        b.Property(m => m.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        b.Property(m => m.RequiresAuth).HasColumnName("requires_auth").HasDefaultValue(true);
        b.Property(m => m.DefaultDestination).HasColumnName("default_destination").HasMaxLength(16);
        b.Property(m => m.MaxConcurrentConnections).HasColumnName("max_concurrent_conn").HasDefaultValue(5);
        b.Property(m => m.Timeout).HasColumnName("timeout_minutes")
         .HasConversion(
             ts => (int)ts.TotalMinutes,
             mins => TimeSpan.FromMinutes(mins));
        b.Property(m => m.Manufacturer).HasColumnName("manufacturer").HasMaxLength(100);
        b.Property(m => m.ModelName).HasColumnName("model_name").HasMaxLength(100);
        b.Property(m => m.Location).HasColumnName("location").HasMaxLength(200);
        b.Property(m => m.Department).HasColumnName("department").HasMaxLength(100);
        b.Property(m => m.LastConnectionAt).HasColumnName("last_connection_at");
        b.Property(m => m.IsOnline).HasColumnName("is_online").HasDefaultValue(false);

        // Shadow properties for timestamps
        b.Property<DateTime>("created_at").HasDefaultValueSql("datetime('now')");
        b.Property<DateTime>("updated_at").HasDefaultValueSql("datetime('now')");
        b.Property<DateTime?>("synced_from_hub_at").HasColumnName("synced_from_hub_at");

        b.HasIndex(m => m.AETitle).IsUnique().HasDatabaseName(IndexNames.ModalityAeTitle);
        b.HasIndex(m => m.IsEnabled).HasDatabaseName(IndexNames.ModalityEnabled);
    }
}
