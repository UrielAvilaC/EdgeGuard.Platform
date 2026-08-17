using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Notifications;

public sealed class NotificationTemplateService(
    INotificationTemplateRepository repository,
    INotificationVariableResolver resolver,
    IUnitOfWork unitOfWork,
    ILogger<NotificationTemplateService> logger) : INotificationTemplateService
{
    public async Task<IReadOnlyList<NotificationTemplateDto>> GetAllAsync(CancellationToken ct = default)
    {
        var templates = await repository.GetAllAsync(ct);
        return templates.Select(ToDto).ToList();
    }

    public async Task<NotificationTemplateDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var template = await repository.GetByIdAsync(id, ct);
        return template is null ? null : ToDto(template);
    }

    public async Task<NotificationTemplateDto> CreateAsync(CreateNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var template = NotificationTemplate.Create(
            request.Name, ParseFormat(request.Format), request.Subject, request.Body);

        await repository.AddAsync(template, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Email template created: {Id} '{Name}'", template.Id, template.Name);
        return ToDto(template);
    }

    public async Task<NotificationTemplateDto?> UpdateAsync(string id, UpdateNotificationTemplateRequest request, CancellationToken ct = default)
    {
        var template = await repository.GetByIdAsync(id, ct);
        if (template is null) return null;

        template.Update(request.Name, ParseFormat(request.Format), request.Subject, request.Body);
        if (request.IsActive) template.Activate(); else template.Deactivate();

        await repository.UpdateAsync(template, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(template);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        if (deleted) await unitOfWork.SaveChangesAsync(ct);
        return deleted;
    }

    public PreviewTemplateResponse Preview(PreviewTemplateRequest request)
    {
        var values = resolver.SampleValues();
        return new PreviewTemplateResponse
        {
            Subject = resolver.Render(request.Subject, values),
            Body = resolver.Render(request.Body, values),
        };
    }

    public IReadOnlyList<NotificationTagDto> GetTags() =>
        NotificationTags.All
            .Select(t => new NotificationTagDto { Tag = t.Tag, Description = t.Description, Example = t.Example })
            .ToList();

    private static ReportFormat ParseFormat(string? format) =>
        string.Equals(format, "PlainText", StringComparison.OrdinalIgnoreCase)
            ? ReportFormat.PlainText
            : ReportFormat.Html;

    private static NotificationTemplateDto ToDto(NotificationTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Format = t.Format.ToString(),
        Subject = t.Subject,
        Body = t.Body,
        IsActive = t.IsActive,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
    };
}
