namespace Dicom.Edge.Contracts.Notifications;

/// <summary>Request to deliver a study's results over email and/or WhatsApp.</summary>
public sealed record DeliverResultsRequest
{
    public string[] Emails { get; init; } = [];
    public string[] Phones { get; init; } = [];
    public bool AttachPdf { get; init; }
    public bool IncludeQr { get; init; }
    public string? EmailTemplateId { get; init; }
    public string? WhatsAppTemplateId { get; init; }
}

public sealed record DeliverResultsResponse
{
    public int Enqueued { get; init; }
}

/// <summary>A historical delivery record for a study (one row of the notification outbox).</summary>
public sealed record DeliveryHistoryDto
{
    public required string Id { get; init; }
    public required string Channel { get; init; }
    public required string To { get; init; }
    public required string Status { get; init; }
    public DateTime? SentAt { get; init; }
    public string? Error { get; init; }
    public DateTime CreatedAt { get; init; }
}
