namespace Dicom.Edge.Node.Persistence.EntityConfigurations;

internal sealed class RoutingRuleConfiguration : IEntityTypeConfiguration<RoutingRule>
{
    public void Configure(EntityTypeBuilder<RoutingRule> b)
    {
        b.ToTable(TableNames.RoutingRules);
        b.HasKey(r => r.Id);

        b.Property(r => r.Id).HasColumnName("id").HasMaxLength(36).IsRequired();
        b.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        b.Property(r => r.Priority).HasColumnName("priority").HasDefaultValue(100);
        b.Property(r => r.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        b.Property(r => r.SourceAeTitle).HasColumnName("source_ae_title").HasMaxLength(16);
        b.Property(r => r.Modality).HasColumnName("modality").HasMaxLength(10);
        b.Property(r => r.StudyDescriptionContains).HasColumnName("study_desc_contains").HasMaxLength(200);
        b.Property(r => r.AccessionNumber).HasColumnName("accession_number").HasMaxLength(50);
        b.Property(r => r.InstitutionName).HasColumnName("institution_name").HasMaxLength(100);
        b.Property(r => r.Department).HasColumnName("department").HasMaxLength(100);
        b.Property(r => r.MinInstanceCount).HasColumnName("min_instance_count");
        b.Property(r => r.MaxInstanceCount).HasColumnName("max_instance_count");
        b.Property(r => r.CustomConditionJson).HasColumnName("custom_condition");
        b.Property(r => r.DestinationAeTitle).HasColumnName("destination_ae").HasMaxLength(16).IsRequired();
        b.Property(r => r.SendToHub).HasColumnName("send_to_hub").HasDefaultValue(true);
        b.Property(r => r.SendToPacs).HasColumnName("send_to_pacs").HasDefaultValue(false);
        b.Property(r => r.ArchiveImmediately).HasColumnName("archive_immediately").HasDefaultValue(false);
        b.Property(r => r.AnonymizeBeforeSending).HasColumnName("anonymize").HasDefaultValue(false);
        b.Property(r => r.MatchCount).HasColumnName("match_count").HasDefaultValue(0);
        b.Property(r => r.LastMatchedAt).HasColumnName("last_matched_at");
        b.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(128).IsRequired();
        b.Property(r => r.UpdatedAt).HasColumnName("updated_at");
        b.Property(r => r.UpdatedBy).HasColumnName("updated_by").HasMaxLength(128);

        b.HasIndex(r => new { r.Priority, r.IsEnabled })
         .HasDatabaseName(IndexNames.RoutingPriority);
    }
}
