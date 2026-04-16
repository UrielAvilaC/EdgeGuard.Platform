using Dicom.Edge.Contracts.WhatsApp;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public interface IWhatsAppTemplateService
{
    Task<IReadOnlyList<WhatsAppTemplateDto>> GetAllAsync(CancellationToken ct = default);
    Task<WhatsAppTemplateDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<WhatsAppTemplateDto> CreateAsync(CreateWhatsAppTemplateRequest request, CancellationToken ct = default);
    Task<WhatsAppTemplateDto> UpdateAsync(string id, UpdateWhatsAppTemplateRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
