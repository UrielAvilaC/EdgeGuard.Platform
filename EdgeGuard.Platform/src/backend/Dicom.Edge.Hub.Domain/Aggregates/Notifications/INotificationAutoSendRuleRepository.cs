namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

public interface IWhatsAppAutoSendRuleRepository
{
    Task<WhatsAppAutoSendRule?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<WhatsAppAutoSendRule?> GetByStudyStatusAsync(string studyStatus, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppAutoSendRule>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppAutoSendRule>> GetEnabledAsync(CancellationToken ct = default);
    Task<WhatsAppAutoSendRule> AddAsync(WhatsAppAutoSendRule rule, CancellationToken ct = default);
    Task UpdateAsync(WhatsAppAutoSendRule rule, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
