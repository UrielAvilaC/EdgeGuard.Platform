using Dicom.Edge.Contracts.WhatsApp;
using Dicom.Edge.Models.Enums;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public interface IWhatsAppNotificationService
{
    Task EvaluateAutoSendAsync(string studyId, StudyStatus newStatus, CancellationToken ct = default);
    Task<SendWhatsAppManualResponse> SendManualAsync(SendWhatsAppManualRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppNotificationDto>> GetByStudyAsync(string studyId, CancellationToken ct = default);
    Task<WhatsAppConfigStatusDto> GetConfigStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppTemplateTagDto>> GetAvailableTagsAsync();
    Task ProcessPendingAsync(int batchSize = 50, CancellationToken ct = default);
}
