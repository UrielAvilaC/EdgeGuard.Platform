using Dicom.Edge.Contracts.WhatsApp;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public interface IWhatsAppAutoSendRuleService
{
    Task<IReadOnlyList<WhatsAppAutoSendRuleDto>> GetAllAsync(CancellationToken ct = default);
    Task<WhatsAppAutoSendRuleDto> CreateAsync(CreateWhatsAppAutoSendRuleRequest request, CancellationToken ct = default);
    Task<WhatsAppAutoSendRuleDto> UpdateAsync(string id, UpdateWhatsAppAutoSendRuleRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
