using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

public sealed class Hl7MessageConfiguration : IEntityTypeConfiguration<Hl7Message>
{
    public void Configure(EntityTypeBuilder<Hl7Message> builder)
    {
        builder.ToTable("hl7_messages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).HasColumnType("text").IsRequired();
        builder.Property(m => m.MessageType).HasMaxLength(20).IsRequired();
        builder.Property(m => m.TriggerEvent).HasMaxLength(20);
        builder.Property(m => m.SendingApplication).HasMaxLength(100);
        builder.Property(m => m.SendingFacility).HasMaxLength(100);
        builder.Property(m => m.MessageControlId).HasMaxLength(50);
        builder.Property(m => m.ClientEndpoint).HasMaxLength(100);
        builder.Property(m => m.Hl7Version).HasMaxLength(10);
        builder.Property(m => m.ErrorMessage).HasMaxLength(2000);

        builder.Property(m => m.PatientId).HasMaxLength(100);
        builder.Property(m => m.PatientName).HasMaxLength(200);
        builder.Property(m => m.AccessionNumber).HasMaxLength(100);
        builder.Property(m => m.PatientPhone).HasMaxLength(32);
        builder.Property(m => m.PatientEmail).HasMaxLength(256);
        builder.Property(m => m.PatientSex).HasMaxLength(16);
        builder.Property(m => m.PatientBirthDate).HasMaxLength(16);
        builder.Property(m => m.StudyDate).HasMaxLength(16);
        builder.Property(m => m.Modality).HasMaxLength(16);
        builder.Property(m => m.ProcedureDescription).HasMaxLength(300);
        builder.Property(m => m.ProcedureId).HasMaxLength(100);

        builder.Property(m => m.TargetNodeId).HasMaxLength(100);
        builder.Property(m => m.TargetNodeName).HasMaxLength(200);
        builder.Property(m => m.DispatchError).HasMaxLength(2000);

        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.DispatchStatus).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.DispatchStatus);
        builder.HasIndex(m => m.ReceivedAt);
        builder.HasIndex(m => m.MessageType);
        builder.HasIndex(m => new { m.DispatchStatus, m.Priority, m.ReceivedAt })
               .HasDatabaseName("ix_hl7_messages_dispatch_queue");
    }
}
