namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Repository interface for WhatsApp notification tracking.
/// </summary>
public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification, CancellationToken ct = default);
    Task UpdateAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetByStudyAsync(string studyId, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetPendingAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>Pending records whose retry backoff is due (NextAttemptAt null or in the past).</summary>
    Task<IReadOnlyList<Notification>> GetDuePendingAsync(int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetByStatusAsync(NotificationStatus status, int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Deletes notifications older than the specified cutoff date in batches. Returns the count deleted.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default);
}
