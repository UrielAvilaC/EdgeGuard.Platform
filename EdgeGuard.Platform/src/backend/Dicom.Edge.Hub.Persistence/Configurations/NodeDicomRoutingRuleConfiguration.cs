using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class NodeDicomRoutingRuleConfiguration : IEntityTypeConfiguration<NodeDicomRoutingRule>
{
    public void Configure(EntityTypeBuilder<NodeDicomRoutingRule> b)
    {
        b.ToTable("node_dicom_routing_rules");
        b.HasKey(r => r.Id);

        b.Property(r => r.Id).HasMaxLength(100).IsRequired();
        b.Property(r => r.NodeId).HasMaxLength(100).IsRequired();
        b.Property(r => r.Name).HasMaxLength(200).IsRequired();
        b.Property(r => r.Priority).HasDefaultValue(100);
        b.Property(r => r.IsEnabled).HasDefaultValue(true);

        b.Property(r => r.MatchModality).HasMaxLength(10);
        b.Property(r => r.MatchSourceAeTitle).HasMaxLength(16);
        b.Property(r => r.MatchInstitution).HasMaxLength(100);
        b.Property(r => r.MatchStudyDesc).HasMaxLength(200);

        b.Property(r => r.DestinationAeTitle).HasMaxLength(16).IsRequired();

        b.HasIndex(r => new { r.NodeId, r.IsEnabled, r.Priority })
         .HasDatabaseName("ix_node_dicom_routing_rules_node_active");

        b.Ignore(r => r.DomainEvents);
    }
}
