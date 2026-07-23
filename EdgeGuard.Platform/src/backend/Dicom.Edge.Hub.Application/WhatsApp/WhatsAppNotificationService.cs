using System.Text.Json;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.WhatsApp;
using Dicom.Edge.Hub.Application.Messaging;
using Dicom.Edge.Hub.Domain.Aggregates.Audit;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Models.Enums;
using AuditEventType = Dicom.Edge.Hub.Domain.Aggregates.Audit.AuditEventType;
using AuditSeverity = Dicom.Edge.Hub.Domain.Aggregates.Audit.AuditSeverity;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.WhatsApp;

public sealed class WhatsAppNotificationService(
    INotificationRepository notificationRepository,
    INotificationAutoSendRuleRepository ruleRepository,
    IWhatsAppTemplateRepository templateRepository,
    IStudyRepository studyRepository,
    IPatientRepository patientRepository,
    ISystemSettingRepository settingRepository,
    IHubAuditLogRepository auditRepository,
    IMessagingProvider messagingProvider,
    IUnitOfWork unitOfWork,
    ILogger<WhatsAppNotificationService> logger) : IWhatsAppNotificationService
{
    public async Task EvaluateAutoSendAsync(string studyId, StudyStatus newStatus, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct) || !await IsAutomaticDeliveryEnabledAsync(ct))
            return;

        var statusName = newStatus.ToString();
        var rule = await ruleRepository.GetByStudyStatusAsync(statusName, ct);
        if (rule is null || !rule.IsEnabled)
            return;

        var template = await templateRepository.GetByIdWithVariablesAsync(rule.TemplateId, ct);
        if (template is null || !template.IsActive)
        {
            await AuditSkippedAsync(studyId, statusName, "Template not found or inactive", ct);
            return;
        }

        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return;

        var patient = study.PatientId is not null
            ? await patientRepository.GetByIdAsync(study.PatientId, ct)
            : null;

        if (patient is null)
        {
            await AuditSkippedAsync(studyId, statusName, "Patient not found", ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(patient.PhoneNumber))
        {
            await AuditSkippedAsync(studyId, statusName, "No phone number for patient", ct);
            return;
        }

        var prefix = await GetSettingValueAsync(HubSettingKeys.WhatsApp.DefaultCountryPrefix, "+521", ct);
        var normalized = PhoneNumberNormalizer.Normalize(patient.PhoneNumber, prefix);
        if (normalized is null)
        {
            await AuditSkippedAsync(studyId, statusName,
                $"Invalid phone format: {PhoneNumberNormalizer.RedactForAudit(patient.PhoneNumber)}", ct);
            return;
        }

        var variables = WhatsAppVariableResolver.Resolve(study, patient, template.Variables);

        var notification = Notification.Create(
            studyId, patient.PhoneNumber, NotificationTriggerSource.Automatic,
            studyStatus: statusName, patientId: patient.Id,
            normalizedPhone: normalized, templateId: template.Id, contentSid: template.ContentSid,
            contentVariables: SerializeContentVariables(variables));

        await SendAndRecordAsync(notification, normalized, template.ContentSid, variables, ct);
    }

    public async Task<SendWhatsAppManualResponse> SendManualAsync(SendWhatsAppManualRequest request, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct))
            throw new InvalidOperationException("WhatsApp messaging is disabled.");

        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppManualSendRequested,
            "Manual WhatsApp send requested",
            entityId: request.StudyId, entityType: "Notification",
            details: $"{{\"studyId\":\"{request.StudyId}\",\"templateId\":\"{request.TemplateId}\",\"recipientCount\":{request.Recipients.Length}}}"), ct);

        var study = await studyRepository.GetByIdAsync(request.StudyId, ct)
            ?? throw new KeyNotFoundException($"Study '{request.StudyId}' not found.");

        var template = await templateRepository.GetByIdWithVariablesAsync(request.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Template '{request.TemplateId}' not found.");

        var patient = study.PatientId is not null
            ? await patientRepository.GetByIdAsync(study.PatientId, ct)
            : null;

        var prefix = await GetSettingValueAsync(HubSettingKeys.WhatsApp.DefaultCountryPrefix, "+521", ct);
        var variables = WhatsAppVariableResolver.Resolve(study, patient, template.Variables);

        var results = new List<WhatsAppSendResultDto>();
        var notificationIds = new List<string>();

        foreach (var recipient in request.Recipients)
        {
            var normalized = PhoneNumberNormalizer.Normalize(recipient.PhoneNumber, prefix);
            if (normalized is null)
            {
                results.Add(new WhatsAppSendResultDto
                {
                    PhoneNumber = recipient.PhoneNumber,
                    Success = false,
                    Error = "Invalid phone number format"
                });
                continue;
            }

            var notification = Notification.Create(
                request.StudyId, recipient.PhoneNumber, NotificationTriggerSource.Manual,
                studyStatus: study.Status.ToString(), patientId: study.PatientId,
                normalizedPhone: normalized, templateId: template.Id, contentSid: template.ContentSid,
                contentVariables: SerializeContentVariables(variables));

            var sendResult = await SendAndRecordAsync(notification, normalized, template.ContentSid, variables, ct);

            notificationIds.Add(notification.Id);
            results.Add(new WhatsAppSendResultDto
            {
                PhoneNumber = recipient.PhoneNumber,
                NormalizedPhone = normalized,
                NotificationId = notification.Id,
                Success = sendResult.Success,
                Error = sendResult.Error
            });
        }

        return new SendWhatsAppManualResponse
        {
            NotificationIds = notificationIds,
            Succeeded = results.Count(r => r.Success),
            Failed = results.Count(r => !r.Success),
            Results = results
        };
    }

    public async Task<IReadOnlyList<WhatsAppNotificationDto>> GetByStudyAsync(string studyId, CancellationToken ct = default)
    {
        var notifications = await notificationRepository.GetByStudyAsync(studyId, ct);
        return notifications.Select(MapToDto).ToList();
    }

    public async Task<WhatsAppConfigStatusDto> GetConfigStatusAsync(CancellationToken ct = default)
    {
        var enabled = await GetSettingBoolAsync(HubSettingKeys.WhatsApp.Enabled, ct);
        var autoDelivery = await GetSettingBoolAsync(HubSettingKeys.WhatsApp.EnableAutomaticDelivery, ct);
        var provider = await GetSettingValueAsync(HubSettingKeys.WhatsApp.Provider, "Twilio", ct);
        var providerConfigJson = await GetSettingValueAsync(HubSettingKeys.WhatsApp.ProviderConfig, "{}", ct);
        var prefix = await GetSettingValueAsync(HubSettingKeys.WhatsApp.DefaultCountryPrefix, "+521", ct);

        MessagingProviderConfig? config = null;
        try { config = JsonSerializer.Deserialize<MessagingProviderConfig>(providerConfigJson); } catch { }

        var activeTemplates = await templateRepository.GetActiveAsync(ct);
        var activeRules = await ruleRepository.GetEnabledAsync(ct);
        var pending = await notificationRepository.GetByStatusAsync(NotificationStatus.Pending, 0, ct);
        var failed = await notificationRepository.GetByStatusAsync(NotificationStatus.Failed, 0, ct);

        return new WhatsAppConfigStatusDto
        {
            Enabled = enabled,
            AutomaticDeliveryEnabled = autoDelivery,
            Provider = provider,
            ProviderConfigured = config?.IsComplete() ?? false,
            DefaultCountryPrefix = prefix,
            ActiveTemplatesCount = activeTemplates.Count,
            ActiveRulesCount = activeRules.Count,
            PendingNotifications = pending.Count,
            FailedNotifications = failed.Count
        };
    }

    public Task<IReadOnlyList<WhatsAppTemplateTagDto>> GetAvailableTagsAsync()
    {
        IReadOnlyList<WhatsAppTemplateTagDto> tags =
        [
            new() { Tag = WhatsAppTemplateTags.AccessionNumber, Description = "Study accession number", Example = "ACC-2025-001" },
            new() { Tag = WhatsAppTemplateTags.PatientCode, Description = "Patient DICOM ID", Example = "PAT12345" },
            new() { Tag = WhatsAppTemplateTags.PatientName, Description = "Patient full name", Example = "John Doe" },
            new() { Tag = WhatsAppTemplateTags.StudyDate, Description = "Study date (dd/MM/yyyy)", Example = "15/07/2025" },
            new() { Tag = WhatsAppTemplateTags.AppointmentDate, Description = "Appointment/worklist date", Example = "20/07/2025" },
            new() { Tag = WhatsAppTemplateTags.StudyDescription, Description = "Study description", Example = "CT Abdomen" },
            new() { Tag = WhatsAppTemplateTags.Modality, Description = "Imaging modality", Example = "CT" },
            new() { Tag = WhatsAppTemplateTags.ReferringPhysician, Description = "Referring physician name", Example = "Dr. Smith" },
            new() { Tag = WhatsAppTemplateTags.InstitutionName, Description = "Institution name", Example = "General Hospital" },
            new() { Tag = WhatsAppTemplateTags.PacsViewerLink, Description = "PACS viewer URL", Example = "https://pacs.example.com/view/123" },
            new() { Tag = WhatsAppTemplateTags.ImagesUrl, Description = "Images download URL", Example = "https://images.example.com/study/123" },
        ];
        return Task.FromResult(tags);
    }

    public async Task ProcessPendingAsync(int batchSize = 50, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(ct)) return;

        var pending = await notificationRepository.GetPendingAsync(batchSize, ct);
        var maxAttempts = int.Parse(await GetSettingValueAsync(HubSettingKeys.WhatsApp.RetryMaxAttempts, "3", ct));

        foreach (var notification in pending)
        {
            if (notification.Attempts >= maxAttempts)
            {
                notification.MarkSkipped("Max retry attempts exceeded");
                await notificationRepository.UpdateAsync(notification, ct);
                continue;
            }

            if (notification.ContentSid is null || notification.NormalizedPhone is null)
            {
                notification.MarkSkipped("Missing ContentSid or NormalizedPhone");
                await notificationRepository.UpdateAsync(notification, ct);
                continue;
            }

            try
            {
                var result = await messagingProvider.SendContentMessageAsync(
                    notification.NormalizedPhone, notification.ContentSid,
                    DeserializeContentVariables(notification.ContentVariables), ct);

                if (result.Success)
                    notification.MarkSent(messagingProvider.ProviderName, result.ProviderMessageId);
                else
                    notification.MarkFailed(result.Error ?? "Unknown error");

                await notificationRepository.UpdateAsync(notification, ct);
            }
            catch (Exception ex)
            {
                notification.MarkFailed(ex.Message);
                await notificationRepository.UpdateAsync(notification, ct);
                logger.LogWarning(ex, "Failed to process pending notification {Id}", notification.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<SendMessageResult> SendAndRecordAsync(
        Notification notification, string normalizedPhone, string contentSid,
        Dictionary<int, string> variables, CancellationToken ct)
    {
        await notificationRepository.AddAsync(notification, ct);

        try
        {
            var result = await messagingProvider.SendContentMessageAsync(normalizedPhone, contentSid, variables, ct);

            if (result.Success)
            {
                notification.MarkSent(messagingProvider.ProviderName, result.ProviderMessageId);
                await auditRepository.AddAsync(HubAuditLog.Create(
                    AuditEventType.WhatsAppNotificationSent,
                    "WhatsApp notification sent",
                    entityId: notification.Id, entityType: "Notification",
                    details: $"{{\"phone\":\"{PhoneNumberNormalizer.RedactForAudit(normalizedPhone)}\",\"provider\":\"{messagingProvider.ProviderName}\",\"providerMessageId\":\"{result.ProviderMessageId}\"}}"), ct);
            }
            else
            {
                notification.MarkFailed(result.Error ?? "Unknown error");
                await auditRepository.AddAsync(HubAuditLog.Create(
                    AuditEventType.WhatsAppNotificationFailed,
                    "WhatsApp notification failed",
                    severity: AuditSeverity.Warning,
                    entityId: notification.Id, entityType: "Notification",
                    details: $"{{\"phone\":\"{PhoneNumberNormalizer.RedactForAudit(normalizedPhone)}\",\"error\":\"{result.Error}\",\"attempts\":{notification.Attempts}}}"), ct);
            }

            await unitOfWork.SaveChangesAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            notification.MarkFailed(ex.Message);
            await unitOfWork.SaveChangesAsync(ct);
            return new SendMessageResult(false, null, ex.Message);
        }
    }

    /// <summary>Serializes positional variables to the JSON object Twilio ContentVariables expects
    /// (string keys). Returns null when empty. Persisted on the Notification for retry re-hydration.</summary>
    private static string? SerializeContentVariables(Dictionary<int, string> variables) =>
        variables.Count == 0
            ? null
            : JsonSerializer.Serialize(variables.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

    /// <summary>Inverse of <see cref="SerializeContentVariables"/>: rehydrates the persisted JSON.</summary>
    private static Dictionary<int, string> DeserializeContentVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return raw is null
            ? []
            : raw.Where(kv => int.TryParse(kv.Key, out _))
                 .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
    }

    private async Task AuditSkippedAsync(string studyId, string status, string reason, CancellationToken ct)
    {
        await auditRepository.AddAsync(HubAuditLog.Create(
            AuditEventType.WhatsAppNotificationSkipped,
            "WhatsApp notification skipped",
            entityId: studyId, entityType: "Notification",
            details: $"{{\"studyId\":\"{studyId}\",\"studyStatus\":\"{status}\",\"reason\":\"{reason}\"}}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogDebug("Skipped WhatsApp for study {StudyId}: {Reason}", studyId, reason);
    }

    private async Task<bool> IsEnabledAsync(CancellationToken ct) =>
        await GetSettingBoolAsync(HubSettingKeys.WhatsApp.Enabled, ct);

    private async Task<bool> IsAutomaticDeliveryEnabledAsync(CancellationToken ct) =>
        await GetSettingBoolAsync(HubSettingKeys.WhatsApp.EnableAutomaticDelivery, ct);

    private async Task<bool> GetSettingBoolAsync(string key, CancellationToken ct)
    {
        var setting = await settingRepository.GetByKeyAsync(key, ct);
        return setting is not null && bool.TryParse(setting.Value, out var v) && v;
    }

    private async Task<string> GetSettingValueAsync(string key, string defaultValue, CancellationToken ct)
    {
        var setting = await settingRepository.GetByKeyAsync(key, ct);
        return setting?.Value ?? defaultValue;
    }

    private static WhatsAppNotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        StudyId = n.StudyId,
        PatientId = n.PatientId,
        PhoneNumber = n.PhoneNumber,
        NormalizedPhone = n.NormalizedPhone,
        TemplateId = n.TemplateId,
        ContentSid = n.ContentSid,
        StudyStatus = n.StudyStatus,
        Status = n.Status.ToString(),
        TriggeredBy = n.TriggeredBy.ToString(),
        ProviderName = n.ProviderName,
        ProviderMessageId = n.ProviderMessageId,
        SentAt = n.SentAt,
        Attempts = n.Attempts,
        LastError = n.LastError,
        CreatedAt = n.CreatedAt
    };
}
