using System.Text.Json;

namespace Dicom.Edge.Hub.Domain.Entities;

/// <summary>
/// Represents an HL7 message received by the Hub TCP listener.
/// Tracks the full lifecycle: reception → validation → routing → dispatch → delivery.
/// </summary>
public class Hl7Message
{
    // ── Core identity ─────────────────────────────────────────────────────────
    public Guid Id { get; private set; }
    public string Content { get; private set; } = default!;
    public string MessageType { get; private set; } = default!;
    public string? TriggerEvent { get; private set; }
    public string? SendingApplication { get; private set; }
    public string? SendingFacility { get; private set; }
    public string? MessageControlId { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public string? ClientEndpoint { get; private set; }
    public int ReceivedOnPort { get; private set; }

    // ── Processing status ─────────────────────────────────────────────────────
    public Hl7MessageStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // ── Parsed HL7 fields ─────────────────────────────────────────────────────
    public string? PatientId { get; private set; }
    public string? PatientName { get; private set; }
    public string? AccessionNumber { get; private set; }
    public string? Hl7Version { get; private set; }
    public string? PatientPhone { get; private set; }
    public string? PatientEmail { get; private set; }
    public string? PatientSex { get; private set; }
    public string? PatientBirthDate { get; private set; }
    public string? StudyDate { get; private set; }
    public string? Modality { get; private set; }
    public string? ProcedureDescription { get; private set; }
    public string? ProcedureId { get; private set; }

    // ── MRG segment — patient/study merge ────────────────────────────────────
    /// <summary>MRG.1 — Prior patient ID to be merged into <see cref="PatientId"/>.</summary>
    public string? MrgPriorPatientId { get; private set; }

    /// <summary>MRG.7 — Prior patient name (family^given).</summary>
    public string? MrgPriorPatientName { get; private set; }

    /// <summary>MRG.3 — Prior accession number used in ORM order-merge scenarios.</summary>
    public string? MrgPriorAccessionNumber { get; private set; }

    /// <summary>True when the message contains a MRG segment with a prior patient ID.</summary>
    public bool HasMrgSegment => !string.IsNullOrEmpty(MrgPriorPatientId);

    // ── ORU OBX image links ───────────────────────────────────────────────────
    /// <summary>
    /// JSON-serialized list of image/report URLs extracted from ORU OBX segments
    /// where OBX-2 is "RP" (Reference Pointer) or "ED" (Encapsulated Data), or
    /// OBX-5 contains an http/https/wado URL.
    /// </summary>
    public string? ImageLinksJson { get; private set; }

    /// <summary>Deserialized view of <see cref="ImageLinksJson"/>.</summary>
    public IReadOnlyList<string> ImageLinks =>
        string.IsNullOrEmpty(ImageLinksJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<List<string>>(ImageLinksJson) ?? [];

    // ── Dispatch lifecycle ────────────────────────────────────────────────────
    public Hl7DispatchStatus DispatchStatus { get; private set; }
    public string? TargetNodeId { get; private set; }
    public string? TargetNodeName { get; private set; }
    public int Priority { get; private set; }
    public DateTime? ValidatedAt { get; private set; }
    public DateTime? RoutedAt { get; private set; }
    public DateTime? QueuedAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public int DispatchAttempts { get; private set; }
    public string? DispatchError { get; private set; }

    private Hl7Message() { }

    public static Hl7Message Create(string content, string clientEndpoint, int receivedOnPort = 0)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty", nameof(content));

        var cleanContent = content.Replace("\v", "").Replace("\x1C", "");

        var imageLinks = ExtractObxImageLinks(cleanContent);

        var messageType  = ExtractField(cleanContent, "MSH", 8)?.Split('^').FirstOrDefault() ?? "UNKNOWN";
        var triggerEvent = ExtractTriggerEvent(cleanContent);

        // P0-6: For ADT^A40 (Merge Patient), the SURVIVING patient is the LAST PID
        // segment BEFORE the MRG segment (HL7 v2 spec). Using the first PID here would
        // silently swap prior/surviving on senders that emit "merge-context" PID first —
        // a critical patient safety bug. For all other message types, the first PID is used.
        var isAdtA40 = string.Equals(messageType, "ADT", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(triggerEvent, "A40", StringComparison.OrdinalIgnoreCase);

        var patientId   = isAdtA40
            ? ExtractSurvivingPatientField(cleanContent, 3, componentIndex: 0)
            : ExtractSubField(cleanContent, "PID", 3, componentIndex: 0);
        var patientName = isAdtA40
            ? ExtractSurvivingPatientField(cleanContent, 5)
            : ExtractField(cleanContent, "PID", 5);

        return new Hl7Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            MessageType = messageType,
            TriggerEvent = triggerEvent,
            SendingApplication = ExtractField(cleanContent, "MSH", 2),
            SendingFacility = ExtractField(cleanContent, "MSH", 3),
            MessageControlId = ExtractField(cleanContent, "MSH", 9),
            Hl7Version = ExtractField(cleanContent, "MSH", 11),
            PatientId = patientId,
            PatientName = patientName,
            AccessionNumber = ExtractSubField(cleanContent, "OBR", 2, componentIndex: 0),
            StudyDate = ExtractField(cleanContent, "OBR", 7),
            Modality = ExtractField(cleanContent, "OBR", 24),
            ProcedureDescription = ExtractSubField(cleanContent, "OBR", 4, componentIndex: 1),
            ProcedureId = ExtractSubField(cleanContent, "OBR", 4, componentIndex: 0),
            PatientPhone = ExtractPhoneFromPid(cleanContent),
            PatientEmail = ExtractEmailFromPid(cleanContent),
            PatientSex = ExtractField(cleanContent, "PID", 8),
            PatientBirthDate = ExtractField(cleanContent, "PID", 7),
            // MRG segment
            MrgPriorPatientId = ExtractSubField(cleanContent, "MRG", 1, componentIndex: 0),
            MrgPriorPatientName = ExtractSubField(cleanContent, "MRG", 7, componentIndex: 0),
            MrgPriorAccessionNumber = ExtractSubField(cleanContent, "MRG", 3, componentIndex: 0),
            // OBX image links
            ImageLinksJson = imageLinks.Count > 0
                ? JsonSerializer.Serialize(imageLinks)
                : null,
            ReceivedAt = DateTime.UtcNow,
            ClientEndpoint = clientEndpoint,
            ReceivedOnPort = receivedOnPort,
            Status = Hl7MessageStatus.Received,
            DispatchStatus = Hl7DispatchStatus.PendingValidation,
            Priority = 5
        };
    }

    // ── Processing lifecycle ──────────────────────────────────────────────────

    public void MarkAsProcessing()
    {
        if (Status != Hl7MessageStatus.Received)
            throw new InvalidOperationException($"Cannot process message in status {Status}");
        Status = Hl7MessageStatus.Processing;
    }

    public void MarkAsProcessed()
    {
        if (Status != Hl7MessageStatus.Processing)
            throw new InvalidOperationException($"Cannot mark as processed message in status {Status}");
        Status = Hl7MessageStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = Hl7MessageStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }

    // ── Dispatch lifecycle ────────────────────────────────────────────────────

    public void MarkAsValidated()
    {
        DispatchStatus = Hl7DispatchStatus.Validated;
        ValidatedAt = DateTime.UtcNow;
    }

    public void MarkAsValidationFailed(string error)
    {
        DispatchStatus = Hl7DispatchStatus.ValidationFailed;
        ErrorMessage = error;
    }

    public void MarkAsRouted(string targetNodeId, string? targetNodeName, int priority)
    {
        DispatchStatus = Hl7DispatchStatus.Routed;
        TargetNodeId = targetNodeId;
        TargetNodeName = targetNodeName;
        Priority = priority;
        RoutedAt = DateTime.UtcNow;
    }

    public void MarkAsQueued()
    {
        DispatchStatus = Hl7DispatchStatus.Queued;
        QueuedAt = DateTime.UtcNow;
    }

    public void MarkAsDispatching()
    {
        DispatchStatus = Hl7DispatchStatus.Dispatching;
        DispatchAttempts++;
    }

    public void MarkAsDelivered()
    {
        DispatchStatus = Hl7DispatchStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        DispatchError = null;
    }

    public void MarkAsDeliveryFailed(string error)
    {
        DispatchStatus = Hl7DispatchStatus.DeliveryFailed;
        DispatchError = error;
    }

    public void RequeueForDispatch()
    {
        DispatchStatus = Hl7DispatchStatus.Queued;
        DispatchError = null;
    }

    // ── Segment/field extraction ──────────────────────────────────────────────

    private static string? ExtractField(string content, string segmentId, int fieldIndex)
    {
        try
        {
            var segments = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var segment = segments.FirstOrDefault(
                s => s.StartsWith(segmentId + "|", StringComparison.OrdinalIgnoreCase));
            if (segment is null) return null;

            var fields = segment.Split('|');
            return fields.Length > fieldIndex ? NullIfEmpty(fields[fieldIndex]) : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractTriggerEvent(string content)
    {
        var msgType = ExtractField(content, "MSH", 8);
        if (msgType is null) return null;
        var parts = msgType.Split('^');
        return parts.Length > 1 ? NullIfEmpty(parts[1]) : null;
    }

    /// <summary>
    /// Extracts a sub-component from an HL7 field.
    /// Fields: |, Components: ^, Repetitions: ~
    /// </summary>
    private static string? ExtractSubField(string content, string segmentId,
        int fieldIndex, int componentIndex = 0, int repetitionIndex = 0)
    {
        var field = ExtractField(content, segmentId, fieldIndex);
        if (field is null) return null;

        var repetitions = field.Split('~');
        if (repetitionIndex >= repetitions.Length) return null;

        var components = repetitions[repetitionIndex].Split('^');
        return components.Length > componentIndex ? NullIfEmpty(components[componentIndex]) : null;
    }

    /// <summary>
    /// P0-6: For ADT^A40 (Merge Patient), returns a field from the SURVIVING PID
    /// — i.e. the LAST PID segment that PRECEDES the MRG segment, per HL7 v2 spec.
    /// Falls back to first PID when no MRG segment is present.
    /// Returns null if no PID is found.
    /// </summary>
    private static string? ExtractSurvivingPatientField(string content, int fieldIndex, int componentIndex = -1)
    {
        try
        {
            var segments = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

            // Locate MRG segment (the "prior patient" boundary).
            var mrgIndex = -1;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].StartsWith("MRG|", StringComparison.OrdinalIgnoreCase))
                {
                    mrgIndex = i;
                    break;
                }
            }

            // Find the last PID before MRG (or first PID if no MRG).
            string? targetPid = null;
            if (mrgIndex < 0)
            {
                targetPid = segments.FirstOrDefault(
                    s => s.StartsWith("PID|", StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                for (int i = mrgIndex - 1; i >= 0; i--)
                {
                    if (segments[i].StartsWith("PID|", StringComparison.OrdinalIgnoreCase))
                    {
                        targetPid = segments[i];
                        break;
                    }
                }
            }

            if (targetPid is null) return null;

            var fields = targetPid.Split('|');
            if (fieldIndex >= fields.Length) return null;
            var fieldValue = fields[fieldIndex];

            if (componentIndex < 0)
                return NullIfEmpty(fieldValue);

            var components = fieldValue.Split('^');
            return components.Length > componentIndex
                ? NullIfEmpty(components[componentIndex])
                : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Scans all OBX segments in the message and collects observation values that
    /// represent image/report links. Matches OBX where:
    /// <list type="bullet">
    ///   <item>OBX-2 is "RP" (Reference Pointer) or "ED" (Encapsulated Data), OR</item>
    ///   <item>OBX-5 starts with http/https/wado.</item>
    /// </list>
    /// Handles multiple OBX repetitions (multi-OBX ORU^R01).
    /// </summary>
    private static List<string> ExtractObxImageLinks(string content)
    {
        var links = new List<string>();

        var segments = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (!segment.StartsWith("OBX|", StringComparison.OrdinalIgnoreCase))
                continue;

            var fields = segment.Split('|');
            if (fields.Length < 6) continue;

            // OBX-2 (value type) is at index 2; OBX-5 (observation value) at index 5
            var valueType = fields.Length > 2 ? fields[2].Trim() : string.Empty;
            var rawValue  = fields.Length > 5 ? fields[5].Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(rawValue)) continue;

            // RP format: application_id^pointer^type^subtype
            // The actual URL is typically the whole OBX-5 or the first component
            var isRefPointer = string.Equals(valueType, "RP", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(valueType, "ED", StringComparison.OrdinalIgnoreCase);
            var looksLikeUrl = rawValue.StartsWith("http://",  StringComparison.OrdinalIgnoreCase)
                            || rawValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                            || rawValue.StartsWith("wado://",  StringComparison.OrdinalIgnoreCase)
                            || rawValue.StartsWith("wadors://",StringComparison.OrdinalIgnoreCase);

            if (!isRefPointer && !looksLikeUrl) continue;

            // For RP type: extract URL from first ^ component if it contains a URL
            var linkValue = rawValue;
            if (isRefPointer && rawValue.Contains('^'))
            {
                var components = rawValue.Split('^');
                // Find the component that looks most like a URL
                linkValue = components.FirstOrDefault(c =>
                    c.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                    c.StartsWith("wado", StringComparison.OrdinalIgnoreCase))
                    ?? components[0];
            }

            if (!string.IsNullOrWhiteSpace(linkValue))
                links.Add(linkValue.Trim());
        }

        return links;
    }

    /// <summary>
    /// Extracts phone from PID-13 component 1 (first repetition that is NOT an email).
    /// Falls back to PID-14 component 1.
    /// </summary>
    private static string? ExtractPhoneFromPid(string content)
    {
        var pid13 = ExtractField(content, "PID", 13);
        if (pid13 is not null)
        {
            foreach (var repetition in pid13.Split('~'))
            {
                var comp1 = NullIfEmpty(repetition.Split('^')[0]);
                if (comp1 is not null && !comp1.Contains('@'))
                    return comp1;
            }
        }

        return ExtractSubField(content, "PID", 14, componentIndex: 0);
    }

    /// <summary>
    /// Scans all PID-13 repetitions for an email address.
    /// Checks component 4 (index 3) for @, or component 1 when telecom type is "Internet"/"NET".
    /// </summary>
    private static string? ExtractEmailFromPid(string content)
    {
        var field = ExtractField(content, "PID", 13);
        if (field is null) return null;

        foreach (var repetition in field.Split('~'))
        {
            var components = repetition.Split('^');

            if (components.Length > 3)
            {
                var comp4 = NullIfEmpty(components[3]);
                if (comp4 is not null && comp4.Contains('@'))
                    return comp4;
            }

            if (components.Length > 2)
            {
                var telecomType = NullIfEmpty(components[2]);
                var value = NullIfEmpty(components[0]);
                if (value is not null && value.Contains('@') &&
                    telecomType is "Internet" or "NET")
                    return value;
            }
        }

        return null;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum Hl7MessageStatus
{
    Received,
    Processing,
    Processed,
    Failed
}

/// <summary>
/// Tracks the dispatch lifecycle of an HL7 message through the Hub pipeline.
/// </summary>
public enum Hl7DispatchStatus
{
    PendingValidation,
    Validated,
    ValidationFailed,
    Routed,
    Queued,
    Dispatching,
    Delivered,
    DeliveryFailed
}
