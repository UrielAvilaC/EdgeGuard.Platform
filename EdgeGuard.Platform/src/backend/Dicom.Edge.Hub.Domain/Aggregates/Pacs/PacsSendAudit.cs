using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Pacs;

/// <summary>
/// Audit record for each PACS C-STORE send attempt. Tracks success, timing, and errors per attempt.
/// </summary>
public sealed class PacsSendAudit : Entity<string>
{
    public string StudyId { get; private set; } = default!;
    public string PacsId { get; private set; } = default!;
    public string? PacsAeTitle { get; private set; }
    public int Attempt { get; private set; }
    public bool Success { get; private set; }
    public string? ResponseCode { get; private set; }
    public long? ResponseTimeMs { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SentAt { get; private set; }

    private PacsSendAudit() { }

    public static PacsSendAudit Create(
        string studyId,
        string pacsId,
        int attempt,
        bool success,
        string? pacsAeTitle = null,
        string? responseCode = null,
        long? responseTimeMs = null,
        string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(studyId))
            throw new ArgumentException("Study ID cannot be empty.", nameof(studyId));
        if (string.IsNullOrWhiteSpace(pacsId))
            throw new ArgumentException("PACS ID cannot be empty.", nameof(pacsId));

        return new PacsSendAudit
        {
            Id = IdGenerator.NewId(),
            StudyId = studyId.Trim(),
            PacsId = pacsId.Trim(),
            PacsAeTitle = pacsAeTitle?.Trim(),
            Attempt = attempt,
            Success = success,
            ResponseCode = responseCode?.Trim(),
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage?.Trim(),
            SentAt = DateTime.UtcNow
        };
    }
}
