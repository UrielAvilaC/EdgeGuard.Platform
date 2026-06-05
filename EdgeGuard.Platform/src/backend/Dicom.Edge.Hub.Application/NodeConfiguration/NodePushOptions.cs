namespace Dicom.Edge.Hub.Application.NodeConfiguration;

/// <summary>
/// Options controlling how Hub→Node configuration pushes are dispatched.
/// Bound from the <c>Push</c> configuration section.
/// </summary>
public sealed class NodePushOptions
{
    public const string SectionName = "Push";

    /// <summary>
    /// When <c>true</c> (default), pushes are enqueued and dispatched off the HTTP
    /// request path (non-blocking; status reported via SignalR <c>NodePushStatus</c>).
    /// When <c>false</c>, pushes run inline (legacy behaviour) — acts as a rollback
    /// switch without a redeploy.
    /// </summary>
    public bool Async { get; set; } = true;
}
