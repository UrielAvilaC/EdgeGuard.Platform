namespace Dicom.Edge.Hub.Domain.Aggregates.Notifications;

public interface IWhatsAppTemplateRepository
{
    Task<WhatsAppTemplate?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<WhatsAppTemplate?> GetByIdWithVariablesAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppTemplate>> GetActiveAsync(CancellationToken ct = default);
    Task<WhatsAppTemplate> AddAsync(WhatsAppTemplate template, CancellationToken ct = default);
    Task UpdateAsync(WhatsAppTemplate template, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
