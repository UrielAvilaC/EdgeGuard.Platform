namespace Dicom.Edge.Hub.Application.Studies;

/// <summary>One real-time study status transition, broadcast to the SPA dashboard / study list.</summary>
public sealed record StudyStatusChange(
    string StudyId,
    string Status,
    string? PatientName,
    string? NodeId,
    DateTime TimestampUtc);

/// <summary>
/// Publishes study status transitions to subscribers (SignalR). Defined in Application so the
/// application services depend only on the abstraction; the transport impl lives in the API layer.
/// </summary>
public interface IStudyRealtimeNotifier
{
    Task StatusChangedAsync(StudyStatusChange change, CancellationToken ct = default);
}
