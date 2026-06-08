using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Application.NodeConfiguration;
using Dicom.Edge.Hub.Domain.Aggregates.Equipment;
using Dicom.Edge.Hub.Domain.Aggregates.Modalities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Application.Equipment;

/// <summary>
/// Manages the per-node equipment catalog on the Hub. Validates assigned modality codes
/// against the reference catalog (must be supported &amp; active) and pushes the resulting
/// catalog to the node after every write.
/// </summary>
public sealed class NodeEquipmentService(
    INodeEquipmentRepository equipmentRepository,
    IModalityRepository modalityRepository,
    INodeEquipmentPushService pushService,
    INodePushQueue pushQueue,
    IOptions<NodePushOptions> pushOptions,
    IUnitOfWork unitOfWork,
    ILogger<NodeEquipmentService> logger) : INodeEquipmentService
{
    public Task<IReadOnlyList<NodeEquipment>> GetByNodeIdAsync(string nodeId, CancellationToken ct = default) =>
        equipmentRepository.GetByNodeIdAsync(nodeId, ct);

    public async Task<NodeEquipment> CreateAsync(
        string nodeId, CreateNodeEquipmentRequest request, CancellationToken ct = default)
    {
        await ValidateModalityCodesAsync(request.ModalityCodes, ct);

        var equipment = NodeEquipment.Create(
            nodeId,
            request.AeTitle,
            request.DisplayName,
            request.StationAeTitle,
            request.StationName,
            request.IpAddress,
            request.Location,
            request.Department,
            request.Manufacturer,
            request.Model,
            request.Notes);

        equipment.SetModalities(request.ModalityCodes);

        await equipmentRepository.AddAsync(equipment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Equipment created: {EquipmentId} '{AeTitle}' ({ModalityCount} modalities) for node {NodeId}",
            equipment.Id, equipment.AeTitle, equipment.ModalityCodes.Count, nodeId);

        await DispatchAsync(nodeId, ct);
        return equipment;
    }

    public async Task<NodeEquipment?> UpdateAsync(
        string id, UpdateNodeEquipmentRequest request, CancellationToken ct = default)
    {
        var equipment = await equipmentRepository.GetByIdAsync(id, ct);
        if (equipment is null) return null;

        await ValidateModalityCodesAsync(request.ModalityCodes, ct);

        equipment.Update(
            request.AeTitle,
            request.DisplayName,
            request.StationAeTitle,
            request.StationName,
            request.IpAddress,
            request.Location,
            request.Department,
            request.Manufacturer,
            request.Model,
            request.Notes);

        equipment.SetModalities(request.ModalityCodes);

        await equipmentRepository.UpdateAsync(equipment, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Equipment updated: {EquipmentId} for node {NodeId}", id, equipment.NodeId);

        await DispatchAsync(equipment.NodeId, ct);
        return equipment;
    }

    public async Task<bool> EnableAsync(string id, CancellationToken ct = default)
    {
        var equipment = await equipmentRepository.GetByIdAsync(id, ct);
        if (equipment is null) return false;

        equipment.Enable();
        await equipmentRepository.UpdateAsync(equipment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAsync(equipment.NodeId, ct);
        return true;
    }

    public async Task<bool> DisableAsync(string id, CancellationToken ct = default)
    {
        var equipment = await equipmentRepository.GetByIdAsync(id, ct);
        if (equipment is null) return false;

        equipment.Disable();
        await equipmentRepository.UpdateAsync(equipment, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await DispatchAsync(equipment.NodeId, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var equipment = await equipmentRepository.GetByIdAsync(id, ct);
        if (equipment is null) return false;

        var nodeId = equipment.NodeId;
        await equipmentRepository.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Equipment deleted: {EquipmentId} for node {NodeId}", id, nodeId);

        await DispatchAsync(nodeId, ct);
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures every requested code exists in the catalog and is assignable
    /// (supported &amp; active). Throws <see cref="UnsupportedModalityCodesException"/> otherwise.
    /// </summary>
    private async Task ValidateModalityCodesAsync(
        IReadOnlyList<string> requestedCodes, CancellationToken ct)
    {
        var normalized = requestedCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0) return;

        var catalog = await modalityRepository.GetByCodesAsync(normalized, ct);
        var assignable = catalog
            .Where(m => m.IsAssignable)
            .Select(m => m.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var offending = normalized.Where(c => !assignable.Contains(c)).ToList();
        if (offending.Count > 0)
            throw new UnsupportedModalityCodesException(offending);
    }

    /// <summary>
    /// Dispatches the equipment push for a node. When async push is enabled (default)
    /// the request is enqueued and returns immediately; otherwise it runs inline.
    /// </summary>
    private async Task DispatchAsync(string nodeId, CancellationToken ct)
    {
        if (pushOptions.Value.Async)
            pushQueue.Enqueue(new NodePushRequest(nodeId, NodePushKind.Equipment));
        else
            await pushService.PushAsync(nodeId, ct);
    }
}
