using Dicom.Edge.Hub.Application.Patients;
using Dicom.Edge.Contracts.Notifications;
using Dicom.Edge.Hub.Application.Notifications;
using Dicom.Edge.Hub.Domain.Aggregates.Configuration;
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
    INotificationRepository notificationRepository,
    IDeliveryService deliveryService,
    INotificationSettingsService settingsService,
    ILogger<StudyAutoDeliveryHandler> logger) : IDomainEventHandler
{
    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        // Map the incoming domain event to the clinical status(es) whose rules it should trigger.
        // Finalized/Completed/Scheduled have dedicated events; WaitingForReport (liga de
        // imágenes sin reporte) is only signalled via StudyStatusChangedEvent.
        var (studyId, statuses) = domainEvent switch
        {
            StudyScheduledEvent ev => (ev.StudyId, (StudyStatus[])[StudyStatus.Scheduled]),
            StudyCompletedEvent ev => (ev.StudyId, (StudyStatus[])[StudyStatus.Completed]),

            // Finalized subsumes "the study has an image link". When the link and the report
            // arrive together the study jumps straight to Finalized without ever passing
            // through WaitingForReport, so a rule bound to that status would never fire even
            // though its condition holds. Evaluate both.
            //
            // WaitingForImageLinks is deliberately NOT included: it means the link is still
            // missing, which is false for a finalized study.
            StudyFinalizedEvent ev
                => (ev.StudyId, (StudyStatus[])[StudyStatus.Finalized, StudyStatus.WaitingForReport]),

            StudyStatusChangedEvent ev when ev.NewStatus == StudyStatus.WaitingForReport
                => (ev.StudyId, (StudyStatus[])[StudyStatus.WaitingForReport]),

            // Re-sent ORU on a study whose status did not move. Map it to the statuses the
            // artifacts now imply, so the same rules a first-time arrival would have matched
            // run again against the corrected results.
            StudyResultsUpdatedEvent ev => (ev.StudyId, StatusesFor(ev.HasImageLinks, ev.HasReport)),

            _ => (null, []),
        };
        if (studyId is null) return;

        // A results update is an intentional re-notification: the report or the link changed,
        // so the per-template "already delivered" guard must not suppress it.
        var isResultsUpdate = domainEvent is StudyResultsUpdatedEvent;

        var statusNames = statuses.Select(s => s.ToString()).ToArray();

        if (!await settingsService.ResolveAutoModeAsync(ct))
        {
            logger.LogInformation(
                "Auto-delivery skipped for study {StudyId} ({Statuses}): auto-mode is off ({Key})",
                studyId, string.Join("/", statusNames), HubSettingKeys.WhatsApp.EnableAutomaticDelivery);
            return;
        }

        var rules = (await ruleRepository.GetEnabledAsync(ct))
            .Where(r => statusNames.Contains(r.StudyStatus, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (rules.Count == 0)
        {
            logger.LogInformation(
                "Auto-delivery skipped for study {StudyId}: no enabled auto-send rule for {Statuses}",
                studyId, string.Join("/", statusNames));
            return;
        }

        var study = await studyRepository.GetByIdAsync(studyId, ct);
        if (study is null) return;
        var patient = await StudyPatientResolver.ResolveAsync(patientRepository, study, ct);

        // A study can legitimately raise the same rule twice — passing through WaitingForReport
        // and then Finalized in one ORU, or a repeat ORU re-running the recompute. Auto-delivery
        // is once per template per study; manual sends go straight to DeliveryService and are
        // not affected by this.
        var alreadySent = (await notificationRepository.GetByStudyAsync(studyId, ct))
            .Where(n => n.Status != NotificationStatus.Failed && n.TemplateId is not null)
            .Select(n => n.TemplateId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in rules)
        {
            if (!alreadySent.Add(rule.TemplateId) && !isResultsUpdate)
            {
                logger.LogDebug(
                    "Auto-delivery skipped for study {StudyId}: template {TemplateId} already delivered",
                    studyId, rule.TemplateId);
                continue;
            }

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
                // Report which link failed, not just that it failed: the study may carry no
                // patient FK, the FK may point at a row that no longer resolves, or the patient
                // may simply have no value for this channel. Each needs a different fix.
                logger.LogWarning(
                    "Auto-delivery skipped for study {StudyId}: no {Channel} contact " +
                    "(PatientRecordId={PatientRecordId}, MRN={PatientMrn}, resolved={Resolved}, " +
                    "patientRow={PatientRow}, hasPhone={HasPhone}, hasEmail={HasEmail})",
                    studyId, rule.Channel,
                    study.PatientRecordId ?? "<null>",
                    study.PatientId ?? "<null>",
                    patient is not null,
                    patient?.Id ?? "<none>",
                    !string.IsNullOrWhiteSpace(patient?.PhoneNumber),
                    !string.IsNullOrWhiteSpace(patient?.Email));
                continue;
            }

            var result = await deliveryService.DeliverAsync(
                studyId, request, NotificationTriggerSource.Automatic, ct);

            // DeliverAsync returns Enqueued=0 when it could not build a target (missing
            // template, unusable phone). Logging "enqueued" regardless made a study that sent
            // nothing look identical to one that sent fine.
            if (result is null || result.Enqueued == 0)
            {
                logger.LogWarning(
                    "Auto-delivery produced nothing for study {StudyId} (rule status {RuleStatus}, " +
                    "channel {Channel}, template {TemplateId}) — see the delivery warning above",
                    studyId, rule.StudyStatus, rule.Channel, rule.TemplateId);

                // Nothing was sent, so do not let the template count as delivered.
                alreadySent.Remove(rule.TemplateId);
                continue;
            }

            logger.LogInformation(
                "Auto-delivery enqueued for study {StudyId} (rule status {RuleStatus}) via {Channel}: {Count}",
                studyId, rule.StudyStatus, rule.Channel, result.Enqueued);
        }
    }

    /// <summary>
    /// Mirrors <c>Study.RecomputeCompletion</c>: the clinical statuses a study holding these
    /// artifacts would be in. Kept in step with it so a re-sent ORU matches exactly the rules
    /// the first delivery matched.
    /// </summary>
    private static StudyStatus[] StatusesFor(bool hasImageLinks, bool hasReport) =>
        (hasImageLinks, hasReport) switch
        {
            (true, true)  => [StudyStatus.Finalized, StudyStatus.WaitingForReport],
            (false, true) => [StudyStatus.WaitingForImageLinks],
            (true, false) => [StudyStatus.WaitingForReport],
            _             => [],
        };

    private static string[] Arr(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value];
}
