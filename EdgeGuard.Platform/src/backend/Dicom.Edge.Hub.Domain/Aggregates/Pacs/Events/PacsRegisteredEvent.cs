using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs.Events;

public sealed record PacsRegisteredEvent(
    string PacsId,
    string AeTitle,
    string HostName,
    int Port) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
