namespace Dicom.Edge.Hub.Application.Queue;

/// <summary>
/// Configurable queue and dispatch policies.
/// </summary>
public sealed class MessageQueueOptions
{
    public const string SectionName = "MessageQueue";

    public bool DispatchEnabled { get; set; } = true;
    public int DispatchBatchSize { get; set; } = 20;
    public int DispatchIntervalSeconds { get; set; } = 5;
    public int MaxRetries { get; set; } = 5;
    public int RetryDelaySeconds { get; set; } = 30;
    public int DispatchTimeoutSeconds { get; set; } = 30;
    public int MaxPendingMessages { get; set; } = 10000;
    public int RetentionDays { get; set; } = 30;
}
