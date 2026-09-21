using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public class PacsServerConfiguration : IEntityTypeConfiguration<PacsServer>
{
    public void Configure(EntityTypeBuilder<PacsServer> builder)
    {
        builder.ToTable("pacs_servers");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(50);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(128);
        builder.Property(p => p.HostName).IsRequired().HasMaxLength(256);
        builder.Property(p => p.Description).HasMaxLength(512);
        builder.Property(p => p.SupportedModalitiesCsv).HasMaxLength(256);

        builder.OwnsOne(p => p.AeTitle, vo =>
        {
            vo.Property(v => v.Value)
              .HasColumnName("ae_title")
              .IsRequired()
              .HasMaxLength(16);

            // Único sólo entre los PACS vivos: uno eliminado conserva su fila y no debe
            // seguir reservando su AE. Ver la nota equivalente en NodeConfiguration.
            vo.HasIndex(v => v.Value)
              .IsUnique()
              .HasFilter("is_deleted = false");
        });

        builder.HasIndex(p => p.IsGlobal);
        builder.HasIndex(p => p.IsEnabled);
        builder.HasIndex(p => p.IsDeleted)
               .HasDatabaseName("ix_pacs_servers_is_deleted");

        builder.Ignore(p => p.DomainEvents);
    }
}
