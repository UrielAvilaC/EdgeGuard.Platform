using Dicom.Edge.Hub.Application.Constants;
using Dicom.Edge.Hub.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7.Pipeline;

/// <summary>
/// Validates HL7 messages with enterprise rules for ADT, ORM, and ORU message types.
/// </summary>
public sealed class Hl7ValidationService : IHl7ValidationService
{
    private static readonly HashSet<string> SupportedMessageTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Hl7ValidationConstants.AdtMessageType,
            Hl7ValidationConstants.OrmMessageType,
            Hl7ValidationConstants.OruMessageType
        };

    private static readonly Dictionary<string, string[]> RequiredSegments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Hl7ValidationConstants.AdtMessageType] =
                [Hl7ValidationConstants.MshSegment, Hl7ValidationConstants.EvnSegment, Hl7ValidationConstants.PidSegment, Hl7ValidationConstants.Pv1Segment],
            [Hl7ValidationConstants.OrmMessageType] =
                [Hl7ValidationConstants.MshSegment, Hl7ValidationConstants.PidSegment, Hl7ValidationConstants.OrcSegment, Hl7ValidationConstants.ObrSegment],
            [Hl7ValidationConstants.OruMessageType] =
                [Hl7ValidationConstants.MshSegment, Hl7ValidationConstants.PidSegment, Hl7ValidationConstants.ObrSegment, Hl7ValidationConstants.ObxSegment],
        };

    private readonly ILogger<Hl7ValidationService> _logger;

    public Hl7ValidationService(ILogger<Hl7ValidationService> logger) => _logger = logger;

    public Hl7ValidationResult Validate(Hl7Message message)
    {
        var warnings = new List<string>();
        var content = message.Content?.Replace("\v", "").Replace("\x1C", "") ?? "";

        // 1. Message type is supported
        var msgType = message.MessageType;
        if (string.IsNullOrWhiteSpace(msgType) || msgType == Hl7ValidationConstants.UnknownMessageType)
            return Hl7ValidationResult.Failure(Hl7ValidationConstants.MshMessageTypeError);

        var baseType = msgType.Split('^').FirstOrDefault() ?? msgType;
        if (!SupportedMessageTypes.Contains(baseType))
            return Hl7ValidationResult.Failure(
                string.Format(Hl7ValidationConstants.UnsupportedTypeTemplate, baseType));

        // 2. Required segments present
        if (RequiredSegments.TryGetValue(baseType, out var required))
        {
            var segments = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Split('|').FirstOrDefault() ?? "")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var seg in required)
            {
                if (!segments.Contains(seg))
                    return Hl7ValidationResult.Failure(
                        string.Format(Hl7ValidationConstants.MissingSegmentTemplate, seg, baseType));
            }
        }

        // 3. MSH required fields
        if (string.IsNullOrWhiteSpace(message.SendingApplication))
            warnings.Add(Hl7ValidationConstants.MshSendingAppWarning);
        if (string.IsNullOrWhiteSpace(message.SendingFacility))
            warnings.Add(Hl7ValidationConstants.MshSendingFacilityWarning);

        // 4. Patient identification
        if (string.IsNullOrWhiteSpace(message.PatientId))
            return Hl7ValidationResult.Failure(Hl7ValidationConstants.PidPatientIdError);

        // 5. Type-specific validations
        if (baseType == Hl7ValidationConstants.OrmMessageType || baseType == Hl7ValidationConstants.OruMessageType)
        {
            if (string.IsNullOrWhiteSpace(message.AccessionNumber))
                warnings.Add(string.Format(Hl7ValidationConstants.ObrAccessionWarningTemplate, baseType));
        }

        if (string.IsNullOrWhiteSpace(message.PatientName))
            warnings.Add(Hl7ValidationConstants.PidPatientNameWarning);

        _logger.LogDebug(
            "HL7 validation passed for {MessageId} ({MessageType}) with {WarningCount} warnings",
            message.Id, msgType, warnings.Count);

        return Hl7ValidationResult.Success(warnings);
    }
}
