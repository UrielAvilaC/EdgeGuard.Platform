using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs.Events;

public sealed record PacsStatusChangedEvent(
    string PacsId,
    string AeTitle,
    bool IsEnabled) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
