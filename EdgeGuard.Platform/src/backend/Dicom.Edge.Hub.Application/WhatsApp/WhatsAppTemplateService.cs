using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.WhatsApp;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public sealed class WhatsAppTemplateService(
    IWhatsAppTemplateRepository templateRepository,
    IHubAuditLogRepository auditRepository,
    IUnitOfWork unitOfWork,
    ILogger<WhatsAppTemplateService> logger) : IWhatsAppTemplateService
{
    public async Task<IReadOnlyList<WhatsAppTemplateDto>> GetAllAsync(CancellationToken ct = default)
    {
        var templates = await templateRepository.GetAllAsync(ct);
        return templates.Select(MapToDto).ToList();
    }

    public async Task<WhatsAppTemplateDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdWithVariablesAsync(id, ct);
        return template is null ? null : MapToDto(template);
    }

    public async Task<WhatsAppTemplateDto> CreateAsync(CreateWhatsAppTemplateRequest request, CancellationToken ct = default)
    {
        var template = WhatsAppTemplate.Create(request.Name, request.ContentSid, request.Description);
        template.SetVariables(request.Tags);

        await templateRepository.AddAsync(template, ct);
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppTemplateCreated,
            $"WhatsApp template '{request.Name}' created",
            entityId: template.Id, entityType: "WhatsAppTemplate",
            details: $"{{\"templateId\":\"{template.Id}\",\"name\":\"{request.Name}\",\"contentSid\":\"{request.ContentSid}\",\"variableCount\":{request.Tags.Length}}}"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Created WhatsApp template {TemplateId} '{Name}'", template.Id, request.Name);
        return MapToDto(template);
    }

    public async Task<WhatsAppTemplateDto> UpdateAsync(string id, UpdateWhatsAppTemplateRequest request, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdWithVariablesAsync(id, ct)
            ?? throw new KeyNotFoundException($"Template '{id}' not found.");

        template.Update(request.Name, request.ContentSid, request.Description);
        template.SetVariables(request.Tags);

        await templateRepository.UpdateAsync(template, ct);
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppTemplateUpdated,
            $"WhatsApp template '{request.Name}' updated",
            entityId: id, entityType: "WhatsAppTemplate"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Updated WhatsApp template {TemplateId}", id);
        return MapToDto(template);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Template '{id}' not found.");

        await templateRepository.DeleteAsync(id, ct);
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppTemplateDeleted,
            $"WhatsApp template '{template.Name}' deleted",
            severity: AuditSeverity.Warning,
            entityId: id, entityType: "WhatsAppTemplate"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Deleted WhatsApp template {TemplateId}", id);
    }

    private static WhatsAppTemplateDto MapToDto(WhatsAppTemplate t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        ContentSid = t.ContentSid,
        Description = t.Description,
        IsActive = t.IsActive,
        Variables = t.Variables.Select(v => new WhatsAppTemplateVariableDto { Position = v.Position, Tag = v.Tag }).ToList(),
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
