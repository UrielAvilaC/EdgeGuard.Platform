using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Patients;
using Dicom.Edge.Hub.Domain.Aggregates.Studies;
using Dicom.Edge.Hub.Domain.Aggregates.Studies.Events;
using Dicom.Edge.Hub.Domain.Common;
using Dicom.Edge.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.EventHandlers;

/// <summary>
/// On a study reaching one of the clinical notification statuses (Scheduled, Completed,
/// WaitingForReport, Finalized), when auto-mode is enabled, enqueues automatic results
/// delivery for each enabled auto-send rule bound to that status (per channel) using the
/// patient's contact details. Runs in the domain-event dispatch scope (post-save), so the
/// delivery enqueue uses its own DbContext — no SaveChanges re-entrancy.
/// </summary>
public sealed class StudyAutoDeliveryHandler(
    INotificationAutoSendRuleRepository ruleRepository,
    IStudyRepository studyRepository,
    IPatientRepository patientRepository,
    IDeliveryService deliveryService,
    INotificationSettingsService settingsService,
    ILogger<StudyAutoDeliveryHandler> logger) : IDomainEventHandler
{
    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        // Map the incoming domain event to the clinical status whose rules it should trigger.
        // Finalized/Completed/Scheduled have dedicated events; WaitingForReport (liga de
        // imágenes sin reporte) is only signalled via StudyStatusChangedEvent.
        var (studyId, status) = domainEvent switch
        {
            StudyScheduledEvent ev => (ev.StudyId, StudyStatus.Scheduled),
            StudyCompletedEvent ev => (ev.StudyId, StudyStatus.Completed),
            StudyFinalizedEvent ev => (ev.StudyId, StudyStatus.Finalized),
            StudyStatusChangedEvent ev when ev.NewStatus == StudyStatus.WaitingForReport
                => (ev.StudyId, StudyStatus.WaitingForReport),
            _ => (null, default),
        };
        if (studyId is null) return;
        if (!await settingsService.ResolveAutoModeAsync(ct)) return;

        var statusName = status.ToString();
        var rules = (await ruleRepository.GetEnabledAsync(ct))
            .Where(r => string.Equals(r.StudyStatus, statusName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (rules.Count == 0) return;

        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return;
        var patient = study.PatientId is not null
            ? await patientRepository.GetByIdAsync(study.PatientId, ct)
            : null;

        foreach (var rule in rules)
        {
            var isEmail = string.Equals(rule.Channel, "Email", StringComparison.OrdinalIgnoreCase);
            var request = isEmail
                ? new DeliverResultsRequest
                {
                    Emails = Arr(patient?.Email),
                    EmailTemplateId = rule.TemplateId,
                    AttachPdf = rule.AttachPdf,
                    IncludeQr = rule.IncludeQr,
                }
                : new DeliverResultsRequest
                {
                    Phones = Arr(patient?.PhoneNumber),
                    WhatsAppTemplateId = rule.TemplateId,
                    AttachPdf = rule.AttachPdf,
                    IncludeQr = rule.IncludeQr,
                };

            if (request.Emails.Length + request.Phones.Length == 0)
            {
                logger.LogWarning("Auto-delivery skipped for study {StudyId}: no {Channel} contact", studyId, rule.Channel);
                continue;
            }

            await deliveryService.DeliverAsync(studyId, request, ct);
            logger.LogInformation("Auto-delivery enqueued for study {StudyId} ({Status}) via {Channel}", studyId, statusName, rule.Channel);
        }
    }

    private static string[] Arr(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value];
}
