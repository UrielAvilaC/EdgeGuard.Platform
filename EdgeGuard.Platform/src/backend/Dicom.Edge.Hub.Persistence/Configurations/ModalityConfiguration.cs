using Dicom.Edge.Hub.Domain.Aggregates.Modalities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class ModalityConfiguration : IEntityTypeConfiguration<Modality>
{
    public void Configure(EntityTypeBuilder<Modality> b)
    {
        b.ToTable("modalities");
        b.HasKey(m => m.Code);

        b.Property(m => m.Code).HasMaxLength(16).IsRequired();
        b.Property(m => m.DisplayName).HasMaxLength(100).IsRequired();
        b.Property(m => m.IsSupported);
        b.Property(m => m.IsActive).HasDefaultValue(true);
        b.Property(m => m.SortOrder);

        b.HasIndex(m => m.SortOrder).HasDatabaseName("ix_modalities_sort_order");
    }
}
