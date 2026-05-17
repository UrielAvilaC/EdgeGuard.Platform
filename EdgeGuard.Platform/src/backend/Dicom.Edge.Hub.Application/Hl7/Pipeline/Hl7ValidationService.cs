using Dicom.Edge.Hub.Application.Constants;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7.Pipeline;

/// <summary>
/// Validates HL7 messages with enterprise rules for ADT, ORM, and ORU message types.
///
/// ADT: only A01 (Admission) and A40 (Merge Patient) are accepted.
///   • A40 requires a MRG segment with a non-empty MRG.1 (prior patient ID).
///
/// ORM: O01 orders are accepted; MRG segment is optional (patient/study reassignment).
///
/// ORU: R01 observation results are accepted; must include at least one OBX segment with a value.
///   OBX segments with RP/ED value type or URL-like OBX-5 are treated as image links.
/// </summary>
public sealed class Hl7ValidationService : IHl7ValidationService
{
    private static readonly HashSet<string> SupportedMessageTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Hl7ValidationConstants.AdtMessageType,
            Hl7ValidationConstants.OrmMessageType,
            Hl7ValidationConstants.OruMessageType,
        };

    // Required segments per base message type (trigger-event–independent minimums)
    private static readonly Dictionary<string, string[]> RequiredSegments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Hl7ValidationConstants.AdtMessageType] =
            [
                Hl7ValidationConstants.MshSegment,
                Hl7ValidationConstants.EvnSegment,
                Hl7ValidationConstants.PidSegment,
                Hl7ValidationConstants.Pv1Segment,
            ],
            [Hl7ValidationConstants.OrmMessageType] =
            [
                Hl7ValidationConstants.MshSegment,
                Hl7ValidationConstants.PidSegment,
                Hl7ValidationConstants.OrcSegment,
                Hl7ValidationConstants.ObrSegment,
            ],
            [Hl7ValidationConstants.OruMessageType] =
            [
                Hl7ValidationConstants.MshSegment,
                Hl7ValidationConstants.PidSegment,
                Hl7ValidationConstants.ObrSegment,
                Hl7ValidationConstants.ObxSegment,
            ],
        };

    private readonly ILogger<Hl7ValidationService> _logger;

    public Hl7ValidationService(ILogger<Hl7ValidationService> logger) => _logger = logger;

    public Hl7ValidationResult Validate(Hl7Message message)
    {
        var warnings = new List<string>();
        var content = message.Content?.Replace("\v", "").Replace("\x1C", "") ?? "";

        // ── 1. Message type is supported ──────────────────────────────────────
        var msgType = message.MessageType;
        if (string.IsNullOrWhiteSpace(msgType) || msgType == Hl7ValidationConstants.UnknownMessageType)
            return Hl7ValidationResult.Failure(Hl7ValidationConstants.MshMessageTypeError);

        var baseType = msgType.Split('^').FirstOrDefault() ?? msgType;
        if (!SupportedMessageTypes.Contains(baseType))
            return Hl7ValidationResult.Failure(
                string.Format(Hl7ValidationConstants.UnsupportedTypeTemplate, baseType));

        // ── 2. ADT trigger event must be A01 or A40 ───────────────────────────
        if (string.Equals(baseType, Hl7ValidationConstants.AdtMessageType, StringComparison.OrdinalIgnoreCase))
        {
            var trigger = message.TriggerEvent;

            if (string.IsNullOrWhiteSpace(trigger) ||
                !Hl7ValidationConstants.AllowedAdtTriggerEvents.Contains(trigger))
            {
                return Hl7ValidationResult.Failure(
                    string.Format(Hl7ValidationConstants.AdtTriggerNotAllowedTemplate,
                        trigger ?? "missing"));
            }

            // A40 (Merge Patient) requires a MRG segment with a prior patient ID
            if (string.Equals(trigger, Hl7ValidationConstants.AdtMergePatientTrigger,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!message.HasMrgSegment)
                    return Hl7ValidationResult.Failure(Hl7ValidationConstants.AdtMergeRequiresMrgError);

                if (string.IsNullOrWhiteSpace(message.MrgPriorPatientId))
                    return Hl7ValidationResult.Failure(Hl7ValidationConstants.MrgPriorPatientIdMissingError);
            }
        }

        // ── 3. Required segments present ──────────────────────────────────────
        if (RequiredSegments.TryGetValue(baseType, out var required))
        {
            var presentSegments = content
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Split('|').FirstOrDefault() ?? "")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var seg in required)
            {
                if (!presentSegments.Contains(seg))
                    return Hl7ValidationResult.Failure(
                        string.Format(Hl7ValidationConstants.MissingSegmentTemplate, seg, baseType));
            }
        }

        // ── 4. MSH required fields ────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(message.SendingApplication))
            warnings.Add(Hl7ValidationConstants.MshSendingAppWarning);
        if (string.IsNullOrWhiteSpace(message.SendingFacility))
            warnings.Add(Hl7ValidationConstants.MshSendingFacilityWarning);

        // ── 5. Patient identification ─────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(message.PatientId))
            return Hl7ValidationResult.Failure(Hl7ValidationConstants.PidPatientIdError);

        // ── 6. Type-specific validations ──────────────────────────────────────

        // ORM / ORU: accession number strongly recommended
        if (string.Equals(baseType, Hl7ValidationConstants.OrmMessageType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(baseType, Hl7ValidationConstants.OruMessageType, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(message.AccessionNumber))
                warnings.Add(string.Format(Hl7ValidationConstants.ObrAccessionWarningTemplate, baseType));
        }

        // ORM with MRG: if MRG segment is present, prior accession number is recommended
        if (string.Equals(baseType, Hl7ValidationConstants.OrmMessageType, StringComparison.OrdinalIgnoreCase)
            && message.HasMrgSegment
            && string.IsNullOrWhiteSpace(message.MrgPriorAccessionNumber))
        {
            warnings.Add(Hl7ValidationConstants.OrmMrgNoAccessionWarning);
        }

        // ORU: at least one OBX image link is expected (warning, not failure)
        if (string.Equals(baseType, Hl7ValidationConstants.OruMessageType, StringComparison.OrdinalIgnoreCase)
            && message.ImageLinks.Count == 0)
        {
            warnings.Add(Hl7ValidationConstants.OruNoImageLinksWarning);
        }

        if (string.IsNullOrWhiteSpace(message.PatientName))
            warnings.Add(Hl7ValidationConstants.PidPatientNameWarning);

        _logger.LogDebug(
            "HL7 validation passed for {MessageId} ({MessageType}^{TriggerEvent}) with {WarningCount} warnings",
            message.Id, msgType, message.TriggerEvent, warnings.Count);

        return Hl7ValidationResult.Success(warnings);
    }
}
