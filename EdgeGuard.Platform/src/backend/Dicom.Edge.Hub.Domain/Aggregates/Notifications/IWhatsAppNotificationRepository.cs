namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>
/// Repository interface for WhatsApp notification tracking.
/// </summary>
public interface IWhatsAppNotificationRepository
{
    Task<WhatsAppNotification> AddAsync(WhatsAppNotification notification, CancellationToken ct = default);
    Task UpdateAsync(WhatsAppNotification notification, CancellationToken ct = default);
    Task<WhatsAppNotification?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppNotification>> GetByStudyAsync(string studyId, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppNotification>> GetPendingAsync(int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppNotification>> GetByStatusAsync(WhatsAppNotificationStatus status, int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Deletes notifications older than the specified cutoff date in batches. Returns the count deleted.
    /// </summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default);
}
