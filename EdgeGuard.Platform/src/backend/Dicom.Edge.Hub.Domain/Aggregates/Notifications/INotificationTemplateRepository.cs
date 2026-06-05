namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

/// <summary>Repository for Email <see cref="NotificationTemplate"/> records.</summary>
public interface INotificationTemplateRepository
{
    Task<IReadOnlyList<NotificationTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<NotificationTemplate?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<NotificationTemplate> AddAsync(NotificationTemplate template, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplate template, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
