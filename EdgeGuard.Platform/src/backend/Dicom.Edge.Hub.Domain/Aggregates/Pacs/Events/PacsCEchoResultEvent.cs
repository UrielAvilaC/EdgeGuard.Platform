using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs.Events;

public sealed record PacsCEchoResultEvent(
    string PacsId,
    string AeTitle,
    bool Success) : IDomainEvent
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
