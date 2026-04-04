using Dicom.Edge.Hub.Domain.Aggregates.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class Hl7RoutingRuleConfiguration : IEntityTypeConfiguration<Hl7RoutingRule>
{
    public void Configure(EntityTypeBuilder<Hl7RoutingRule> builder)
    {
        builder.ToTable("hl7_routing_rules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasMaxLength(100);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.TargetNodeId).HasMaxLength(100).IsRequired();
        builder.Property(r => r.MatchMessageType).HasMaxLength(20);
        builder.Property(r => r.MatchTriggerEvent).HasMaxLength(20);
        builder.Property(r => r.MatchSendingFacility).HasMaxLength(100);
        builder.Property(r => r.MatchSendingApplication).HasMaxLength(100);

        builder.HasIndex(r => new { r.IsEnabled, r.Priority })
               .HasDatabaseName("ix_hl7_routing_rules_active");

        builder.Ignore(r => r.DomainEvents);
    }
}
