using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;

namespace Dicom.Edge.Hub.Application.Equipment;

public interface INodeEquipmentService
{
    Task<IReadOnlyList<NodeEquipment>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default);
    Task<NodeEquipment> CreateAsync(string nodeId, CreateNodeEquipmentRequest request, CancellationToken ct = default);
    Task<NodeEquipment?> UpdateAsync(string id, UpdateNodeEquipmentRequest request, CancellationToken ct = default);
    Task<bool> EnableAsync(string id, CancellationToken ct = default);
    Task<bool> DisableAsync(string id, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
