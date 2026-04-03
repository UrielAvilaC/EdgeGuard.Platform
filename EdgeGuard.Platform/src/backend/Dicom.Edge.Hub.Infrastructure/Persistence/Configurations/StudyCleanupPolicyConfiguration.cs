using Dicom.Edge.Hub.Domain.Aggregates.Cleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Infrastructure.Persistence.Configurations;

public class StudyCleanupPolicyConfiguration : IEntityTypeConfiguration<StudyCleanupPolicy>
{
    public void Configure(EntityTypeBuilder<StudyCleanupPolicy> builder)
    {
        builder.ToTable("StudyCleanupPolicies");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(50);
        builder.Property(p => p.Modality).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.SpecificNodeIdsCsv).HasMaxLength(2048);

        builder.HasIndex(p => p.Modality);
        builder.HasIndex(p => p.IsEnabled);

        builder.Ignore(p => p.DomainEvents);
    }
}
