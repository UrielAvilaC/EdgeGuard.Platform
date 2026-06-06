namespace Dicom.Edge.Hub.Application.Equipment;

public interface INodeEquipmentPushService
{
    /// <summary>
    /// Pushes the full equipment catalog (with modality codes) for a node via HTTP.
    /// Fire-and-forget safe: logs failures but does not throw.
    /// </summary>
    Task<bool> PushAsync(string nodeId, CancellationToken ct = default);
}
