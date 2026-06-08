using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.WhatsApp;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public sealed class WhatsAppAutoSendRuleService(
    IWhatsAppAutoSendRuleRepository ruleRepository,
    IWhatsAppTemplateRepository templateRepository,
    IHubAuditLogRepository auditRepository,
    IUnitOfWork unitOfWork,
    ILogger<WhatsAppAutoSendRuleService> logger) : IWhatsAppAutoSendRuleService
{
    public async Task<IReadOnlyList<WhatsAppAutoSendRuleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rules = await ruleRepository.GetAllAsync(ct);
        var templates = await templateRepository.GetAllAsync(ct);
        var templateMap = templates.ToDictionary(t => t.Id, t => t.Name);

        return rules.Select(r => MapToDto(r, templateMap.GetValueOrDefault(r.TemplateId))).ToList();
    }

    public async Task<WhatsAppAutoSendRuleDto> CreateAsync(CreateWhatsAppAutoSendRuleRequest request, CancellationToken ct = default)
    {
        var existing = await ruleRepository.GetByStudyStatusAsync(request.StudyStatus, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A rule for status '{request.StudyStatus}' already exists.");

        var template = await templateRepository.GetByIdAsync(request.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template '{request.TemplateId}' not found.");

        var rule = WhatsAppAutoSendRule.Create(request.StudyStatus, request.TemplateId, request.Description);
        await ruleRepository.AddAsync(rule, ct);

        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppAutoSendRuleChanged,
            $"Auto-send rule created for status '{request.StudyStatus}'",
            entityId: rule.Id, entityType: "WhatsAppAutoSendRule",
            details: $"{{\"ruleId\":\"{rule.Id}\",\"studyStatus\":\"{request.StudyStatus}\",\"templateId\":\"{request.TemplateId}\",\"isEnabled\":true}}"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Created auto-send rule {RuleId} for status {StudyStatus}", rule.Id, request.StudyStatus);
        return MapToDto(rule, template.Name);
    }

    public async Task<WhatsAppAutoSendRuleDto> UpdateAsync(string id, UpdateWhatsAppAutoSendRuleRequest request, CancellationToken ct = default)
    {
        var rule = await ruleRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rule '{id}' not found.");

        if (request.TemplateId is not null)
        {
            _ = await templateRepository.GetByIdAsync(request.TemplateId, ct)
                ?? throw new KeyNotFoundException($"Template '{request.TemplateId}' not found.");
            rule.AssignTemplate(request.TemplateId);
        }
        if (request.IsEnabled.HasValue)
        {
            if (request.IsEnabled.Value) rule.Enable(); else rule.Disable();
        }
        if (request.Description is not null)
            rule.UpdateDescription(request.Description);

        await ruleRepository.UpdateAsync(rule, ct);
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppAutoSendRuleChanged,
            $"Auto-send rule updated for status '{rule.StudyStatus}'",
            entityId: id, entityType: "WhatsAppAutoSendRule"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Updated auto-send rule {RuleId}", id);

        var template = await templateRepository.GetByIdAsync(rule.TemplateId, ct);
        return MapToDto(rule, template?.Name);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        _ = await ruleRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Rule '{id}' not found.");

        await ruleRepository.DeleteAsync(id, ct);
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppAutoSendRuleChanged,
            "Auto-send rule deleted",
            severity: AuditSeverity.Warning,
            entityId: id, entityType: "WhatsAppAutoSendRule"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Deleted auto-send rule {RuleId}", id);
    }

    private static WhatsAppAutoSendRuleDto MapToDto(WhatsAppAutoSendRule r, string? templateName) => new()
    {
        Id = r.Id,
        StudyStatus = r.StudyStatus,
        TemplateId = r.TemplateId,
        TemplateName = templateName,
        IsEnabled = r.IsEnabled,
        Description = r.Description,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
