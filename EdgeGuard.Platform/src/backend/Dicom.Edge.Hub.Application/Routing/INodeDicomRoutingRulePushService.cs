namespace Dicom.Edge.Hub.Application.Routing;

public interface INodeDicomRoutingRulePushService
{
    /// <summary>
    /// Pushes the full set of DICOM routing rules for a node via HTTP.
    /// Fire-and-forget safe: logs failures but does not throw.
    /// </summary>
    Task<bool> PushAsync(string nodeId, CancellationToken ct = default);
}
