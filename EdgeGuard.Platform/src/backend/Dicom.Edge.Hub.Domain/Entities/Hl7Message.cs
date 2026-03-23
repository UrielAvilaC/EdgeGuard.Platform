namespace Dicom.Edge.Hub.Domain.Entities;

/// <summary>
/// Representa un mensaje HL7 recibido por el listener.
/// </summary>
public class Hl7Message
{
    public Guid Id { get; private set; }
    public string Content { get; private set; }
    public string MessageType { get; private set; }
    public string? SendingApplication { get; private set; }
    public string? SendingFacility { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public string? ClientEndpoint { get; private set; }
    public Hl7MessageStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Hl7Message() { }

    public static Hl7Message Create(
        string content, 
        string clientEndpoint)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty", nameof(content));

        return new Hl7Message
        {
            Id = Guid.NewGuid(),
            Content = content,
            MessageType = ExtractMessageType(content),
            SendingApplication = ExtractSendingApplication(content),
            SendingFacility = ExtractSendingFacility(content),
            ReceivedAt = DateTime.UtcNow,
            ClientEndpoint = clientEndpoint,
            Status = Hl7MessageStatus.Received
        };
    }

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

    private static string ExtractMessageType(string content)
    {
        try
        {
            var segments = content.Split('\r');
            var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
            if (mshSegment == null) return "UNKNOWN";

            var fields = mshSegment.Split('|');
            return fields.Length > 8 ? fields[8] : "UNKNOWN";
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    private static string? ExtractSendingApplication(string content)
    {
        try
        {
            var segments = content.Split('\r');
            var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
            if (mshSegment == null) return null;

            var fields = mshSegment.Split('|');
            return fields.Length > 2 ? fields[2] : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractSendingFacility(string content)
    {
        try
        {
            var segments = content.Split('\r');
            var mshSegment = segments.FirstOrDefault(s => s.StartsWith("MSH"));
            if (mshSegment == null) return null;

            var fields = mshSegment.Split('|');
            return fields.Length > 3 ? fields[3] : null;
        }
        catch
        {
            return null;
        }
    }
}

public enum Hl7MessageStatus
{
    Received,
    Processing,
    Processed,
    Failed
}
