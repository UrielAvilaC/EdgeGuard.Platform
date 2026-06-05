using Dicom.Edge.Contracts.Notifications;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>CRUD, preview and tag catalog for Email notification templates.</summary>
public interface INotificationTemplateService
{
    Task<IReadOnlyList<NotificationTemplateDto>> GetAllAsync(CancellationToken ct = default);
    Task<NotificationTemplateDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<NotificationTemplateDto> CreateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default);
    Task<NotificationTemplateDto?> UpdateAsync(string id, UpdateNotificationTemplateRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>Renders subject + body with sample data (HTML not sanitized here).</summary>
    PreviewTemplateResponse Preview(PreviewTemplateRequest request);

    IReadOnlyList<NotificationTagDto> GetTags();
}
