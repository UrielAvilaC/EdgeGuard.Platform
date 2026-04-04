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

        return new Hl7Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            MessageType = ExtractField(cleanContent, "MSH", 8)?.Split('^').FirstOrDefault() ?? "UNKNOWN",
            TriggerEvent = ExtractTriggerEvent(cleanContent),
            SendingApplication = ExtractField(cleanContent, "MSH", 2),
            SendingFacility = ExtractField(cleanContent, "MSH", 3),
            MessageControlId = ExtractField(cleanContent, "MSH", 9),
            Hl7Version = ExtractField(cleanContent, "MSH", 11),
            PatientId = ExtractField(cleanContent, "PID", 3),
            PatientName = ExtractField(cleanContent, "PID", 5),
            AccessionNumber = ExtractField(cleanContent, "OBR", 18),
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
            var segment = segments.FirstOrDefault(s => s.StartsWith(segmentId + "|") || s.StartsWith(segmentId));
            if (segment is null) return null;

            var fields = segment.Split('|');
            if (segmentId == "MSH")
                return fields.Length > fieldIndex ? NullIfEmpty(fields[fieldIndex]) : null;

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
        return parts.Length > 1 ? parts[1] : null;
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
