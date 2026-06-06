namespace Dicom.Edge.Hub.Domain.Aggregates.Equipment;

public interface INodeEquipmentRepository
{
    Task<IReadOnlyList<NodeEquipment>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default);
    Task<NodeEquipment?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<NodeEquipment?> GetByNodeAndAeTitleAsync(string nodeId, string aeTitle, CancellationToken ct = default);
    Task AddAsync(NodeEquipment equipment, CancellationToken ct = default);
    Task UpdateAsync(NodeEquipment equipment, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
