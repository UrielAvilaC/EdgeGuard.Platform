using Dicom.Edge.Hub.Application.Patients;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Application.Configuration;
using Dicom.Edge.Hub.Application.WhatsApp;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>Resolves templates and enqueues study-result deliveries (email + WhatsApp).</summary>
public interface IDeliveryService
{
    /// <param name="triggeredBy">
    /// How the delivery was initiated. Defaults to <see cref="NotificationTriggerSource.Manual"/>
    /// so an operator-driven call reads naturally; the auto-send rule handler passes
    /// <see cref="NotificationTriggerSource.Automatic"/>. Recorded on each notification and shown
    /// in the delivery history.
    /// </param>
    Task<DeliverResultsResponse?> DeliverAsync(
        string studyId,
        DeliverResultsRequest request,
        NotificationTriggerSource triggeredBy = NotificationTriggerSource.Manual,
        CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryHistoryDto>> GetHistoryAsync(string studyId, CancellationToken ct = default);
}

public sealed class DeliveryService(
    IStudyRepository studyRepository,
    IPatientRepository patientRepository,
    INodeRepository nodeRepository,
    INotificationTemplateRepository emailTemplateRepository,
    IWhatsAppTemplateRepository whatsAppTemplateRepository,
    INotificationVariableResolver resolver,
    INotificationDispatcher dispatcher,
    INotificationRepository notificationRepository,
    ISystemSettingsService systemSettings,
    ILogger<DeliveryService> logger) : IDeliveryService
{
    public async Task<DeliverResultsResponse?> DeliverAsync(
        string studyId,
        DeliverResultsRequest request,
        NotificationTriggerSource triggeredBy = NotificationTriggerSource.Manual,
        CancellationToken ct = default)
    {
        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return null;

        var patient = await StudyPatientResolver.ResolveAsync(patientRepository, study, ct);

        var facilityName = study.SourceNodeId is not null
            ? (await nodeRepository.GetByIdAsync(study.SourceNodeId, ct))?.Name
            : null;

        var imageLink = SplitFirst(study.ExternalImageLinks);
        var values = resolver.BuildValues(study, patient, imageLink, reportLink: null, facilityName);

        var targets = new List<DeliveryTarget>();

        // ── Email ──
        if (request.Emails.Length > 0 && !string.IsNullOrEmpty(request.EmailTemplateId))
        {
            var template = await emailTemplateRepository.GetByIdAsync(request.EmailTemplateId, ct);
            if (template is null)
            {
                logger.LogWarning(
                    "Delivery for study {StudyId}: email template {TemplateId} not found — no email target built",
                    studyId, request.EmailTemplateId);
            }
            else
            {
                var subject = resolver.Render(template.Subject, values);
                var body = resolver.Render(template.Body, values);
                var isHtml = template.Format == ReportFormat.Html;
                var pdfPath = request.AttachPdf ? study.ReportPdfPath : null;
                var qrLink = request.IncludeQr ? imageLink : null;

                foreach (var email in request.Emails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    targets.Add(new DeliveryTarget
                    {
                        Channel = NotificationChannel.Email,
                        To = email.Trim(),
                        Subject = subject,
                        Body = body,
                        IsHtmlBody = isHtml,
                        AttachmentPath = pdfPath,
                        ImageLink = qrLink,
                        TemplateId = template.Id,
                    });
            }
        }

        // ── WhatsApp (Twilio ContentSid) ──
        if (request.Phones.Length > 0 && !string.IsNullOrEmpty(request.WhatsAppTemplateId))
        {
            // Load WITH variables so we can resolve the positional Content template values —
            // GetByIdAsync alone leaves template.Variables empty and Twilio receives {}.
            var template = await whatsAppTemplateRepository.GetByIdWithVariablesAsync(request.WhatsAppTemplateId, ct);
            if (template is null)
            {
                logger.LogWarning(
                    "Delivery for study {StudyId}: WhatsApp template {TemplateId} not found — no WhatsApp target built",
                    studyId, request.WhatsAppTemplateId);
            }
            else
            {
                // The provider needs E.164. Feeding it the raw stored value (e.g. "55 1234 5678")
                // gets the message rejected at Twilio after burning the retry budget, so normalize
                // here with the same prefix the manual WhatsApp screen uses.
                var prefix = (await systemSettings.GetAsync(
                    HubSettingKeys.WhatsApp.DefaultCountryPrefix, ct))?.Value;
                if (string.IsNullOrWhiteSpace(prefix)) prefix = "+521";

                var whatsAppVars = WhatsAppVariableResolver.Resolve(study, patient, template.Variables, imageLink, facilityName);
                foreach (var phone in request.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                {
                    var normalized = PhoneNumberNormalizer.Normalize(phone, prefix);
                    if (normalized is null)
                    {
                        logger.LogWarning(
                            "Delivery for study {StudyId}: phone '{Phone}' is not a valid number — skipped",
                            studyId, PhoneNumberNormalizer.RedactForAudit(phone));
                        continue;
                    }

                    targets.Add(new DeliveryTarget
                    {
                        Channel = NotificationChannel.WhatsApp,
                        To = phone.Trim(),
                        NormalizedPhone = normalized,
                        ContentSid = template.ContentSid,
                        TemplateId = template.Id,
                        Variables = whatsAppVars,
                    });
                }
            }
        }

        if (targets.Count == 0)
        {
            logger.LogWarning(
                "Delivery for study {StudyId}: nothing to send — no template resolved or no valid recipient",
                studyId);
            return new DeliverResultsResponse { Enqueued = 0 };
        }

        var result = await dispatcher.DeliverAsync(new DeliveryRequest
        {
            StudyId = study.Id,
            PatientId = study.PatientId,
            StudyStatus = study.Status.ToString(),
            TriggeredBy = triggeredBy,
            Targets = targets,
        }, ct);

        logger.LogInformation("Results delivery enqueued for study {StudyId}: {Count}", study.Id, result.Enqueued);
        return new DeliverResultsResponse { Enqueued = result.Enqueued };
    }

    public async Task<IReadOnlyList<DeliveryHistoryDto>> GetHistoryAsync(string studyId, CancellationToken ct = default)
    {
        var items = await notificationRepository.GetByStudyAsync(studyId, ct);
        return items.Select(n => new DeliveryHistoryDto
        {
            Id = n.Id,
            Channel = n.Channel.ToString(),
            To = n.Channel == NotificationChannel.Email ? n.ToEmail ?? "" : n.NormalizedPhone ?? n.PhoneNumber,
            Status = n.Status.ToString(),
            SentAt = n.SentAt,
            Error = n.LastError,
            CreatedAt = n.CreatedAt,
        }).ToList();
    }

    private static string? SplitFirst(string? links) =>
        string.IsNullOrEmpty(links) ? null : links.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
}
