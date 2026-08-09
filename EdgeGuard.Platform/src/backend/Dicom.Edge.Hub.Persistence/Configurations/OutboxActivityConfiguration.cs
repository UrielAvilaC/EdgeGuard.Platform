using Dicom.Edge.Hub.Domain.Aggregates.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dicom.Edge.Hub.Persistence.Configurations;

/// <summary>
/// Maps the keyless <see cref="OutboxActivity"/> projection to the database view
/// <c>vw_outbox_activity</c>. The view itself is created by a raw-SQL migration
/// (AddOutboxActivityView); EF only reads from it.
/// </summary>
public class OutboxActivityConfiguration : IEntityTypeConfiguration<OutboxActivity>
{
    public void Configure(EntityTypeBuilder<OutboxActivity> builder)
    {
        builder.HasNoKey();
        builder.ToView("vw_outbox_activity");
    }
}
