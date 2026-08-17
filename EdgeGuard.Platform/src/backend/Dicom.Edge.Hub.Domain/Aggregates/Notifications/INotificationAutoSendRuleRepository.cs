namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

public interface INotificationAutoSendRuleRepository
{
    Task<NotificationAutoSendRule?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<NotificationAutoSendRule?> GetByStudyStatusAsync(string studyStatus, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationAutoSendRule>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NotificationAutoSendRule>> GetEnabledAsync(CancellationToken ct = default);
    Task<NotificationAutoSendRule> AddAsync(NotificationAutoSendRule rule, CancellationToken ct = default);
    Task UpdateAsync(NotificationAutoSendRule rule, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
