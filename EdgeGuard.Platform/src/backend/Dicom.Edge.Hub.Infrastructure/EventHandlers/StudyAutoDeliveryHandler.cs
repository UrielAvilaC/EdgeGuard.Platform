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
/// On <see cref="StudyFinalizedEvent"/>, when auto-mode is enabled, enqueues automatic
/// results delivery for each enabled <c>Finalized</c> auto-send rule (per channel) using
/// the patient's contact details. Runs in the domain-event dispatch scope (post-save), so
/// the delivery enqueue uses its own DbContext — no SaveChanges re-entrancy.
/// </summary>
public sealed class StudyAutoDeliveryHandler(
    IWhatsAppAutoSendRuleRepository ruleRepository,
    IStudyRepository studyRepository,
    IPatientRepository patientRepository,
    IDeliveryService deliveryService,
    INotificationSettingsService settingsService,
    ILogger<StudyAutoDeliveryHandler> logger) : IDomainEventHandler
{
    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        if (domainEvent is not StudyFinalizedEvent e) return;
        if (!await settingsService.ResolveAutoModeAsync(ct)) return;

        var rules = (await ruleRepository.GetEnabledAsync(ct))
            .Where(r => string.Equals(r.StudyStatus, nameof(StudyStatus.Finalized), StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (rules.Count == 0) return;

        var study = await studyRepository.GetByIdAsync(e.StudyId, ct);
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
                logger.LogWarning("Auto-delivery skipped for study {StudyId}: no {Channel} contact", e.StudyId, rule.Channel);
                continue;
            }

            await deliveryService.DeliverAsync(e.StudyId, request, ct);
            logger.LogInformation("Auto-delivery enqueued for finalized study {StudyId} via {Channel}", e.StudyId, rule.Channel);
        }
    }

    private static string[] Arr(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [value];
}
