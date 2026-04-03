namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class NodeRegistrationConfiguration : IEntityTypeConfiguration<NodeRegistrationEntity>
{
    public void Configure(EntityTypeBuilder<NodeRegistrationEntity> b)
    {
        b.ToTable(TableNames.NodeRegistration);
        b.HasKey(r => r.Id);

        b.Property(r => r.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(r => r.HubNodeId).HasColumnName("hub_node_id").HasMaxLength(64);
        b.Property(r => r.RegistrationStatus).HasColumnName("registration_status").HasMaxLength(32).IsRequired();
        b.Property(r => r.HubBaseUrl).HasColumnName("hub_base_url").HasMaxLength(512);
        b.Property(r => r.HubAssignedVersion).HasColumnName("hub_assigned_version").HasMaxLength(32);
        b.Property(r => r.ErrorMessage).HasColumnName("error_message");
        b.Property(r => r.RegisteredAt).HasColumnName("registered_at");
        b.Property(r => r.LastConfigSyncAt).HasColumnName("last_config_sync_at");
        b.Property(r => r.LastHeartbeatAt).HasColumnName("last_heartbeat_at");
        b.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();
    }
}
