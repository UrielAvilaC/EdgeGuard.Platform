using Dicom.Edge.Contracts.WhatsApp;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public interface IWhatsAppNotificationService
{
    Task<SendWhatsAppManualResponse> SendManualAsync(SendWhatsAppManualRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppNotificationDto>> GetByStudyAsync(string studyId, CancellationToken ct = default);
    Task<WhatsAppConfigStatusDto> GetConfigStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppTemplateTagDto>> GetAvailableTagsAsync();
    Task ProcessPendingAsync(int batchSize = 50, CancellationToken ct = default);
}
