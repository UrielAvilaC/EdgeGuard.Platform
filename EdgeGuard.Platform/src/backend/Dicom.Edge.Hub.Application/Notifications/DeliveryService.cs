using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Application.WhatsApp;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>Resolves templates and enqueues study-result deliveries (email + WhatsApp).</summary>
public interface IDeliveryService
{
    Task<DeliverResultsResponse?> DeliverAsync(string studyId, DeliverResultsRequest request, CancellationToken ct = default);
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
    ILogger<DeliveryService> logger) : IDeliveryService
{
    public async Task<DeliverResultsResponse?> DeliverAsync(
        string studyId, DeliverResultsRequest request, CancellationToken ct = default)
    {
        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return null;

        var patient = study.PatientId is not null
            ? await patientRepository.GetByIdAsync(study.PatientId, ct)
            : null;

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
            if (template is not null)
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
            if (template is not null)
            {
                var whatsAppVars = WhatsAppVariableResolver.Resolve(study, patient, template.Variables, imageLink, facilityName);
                foreach (var phone in request.Phones.Where(p => !string.IsNullOrWhiteSpace(p)))
                    targets.Add(new DeliveryTarget
                    {
                        Channel = NotificationChannel.WhatsApp,
                        To = phone.Trim(),
                        NormalizedPhone = phone.Trim(),
                        ContentSid = template.ContentSid,
                        TemplateId = template.Id,
                        Variables = whatsAppVars,
                    });
            }
        }

        if (targets.Count == 0) return new DeliverResultsResponse { Enqueued = 0 };

        var result = await dispatcher.DeliverAsync(new DeliveryRequest
        {
            StudyId = study.Id,
            PatientId = study.PatientId,
            StudyStatus = study.Status.ToString(),
            TriggeredBy = NotificationTriggerSource.Manual,
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
