using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.HealthChecks;

/// <summary>
/// Result of a C-ECHO verification against a PACS server from a node.
/// </summary>
public sealed class PacsCEchoResult : Entity<string>
{
    public string HealthCheckRecordId { get; private set; } = default!;
    public string PacsId { get; private set; } = default!;
    public string? PacsAeTitle { get; private set; }
    public bool Success { get; private set; }
    public double? ResponseTimeMs { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CheckedAt { get; private set; }

    private PacsCEchoResult() { }

    public static PacsCEchoResult Create(
        string healthCheckRecordId,
        string pacsId,
        bool success,
        string? pacsAeTitle = null,
        double? responseTimeMs = null,
        string? errorMessage = null)
    {
        return new PacsCEchoResult
        {
            Id = IdGenerator.NewId(),
            HealthCheckRecordId = healthCheckRecordId,
            PacsId = pacsId,
            PacsAeTitle = pacsAeTitle,
            Success = success,
            ResponseTimeMs = responseTimeMs,
            ErrorMessage = errorMessage,
            CheckedAt = DateTime.UtcNow
        };
    }
}
