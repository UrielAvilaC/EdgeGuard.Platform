using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;
using Dicom.Edge.Hub.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.PacsServers;

/// <summary>
/// Application service for PacsServer write operations.
/// </summary>
public sealed class PacsServerService(
    IPacsServerRepository pacsRepository,
    IUnitOfWork unitOfWork,
    ILogger<PacsServerService> logger) : IPacsServerService
{
    public async Task<PacsServer> CreateAsync(CreatePacsServerRequest request, CancellationToken ct = default)
    {
        var pacs = PacsServer.Create(
            request.Name,
            AeTitle.Create(request.AeTitle),
            request.HostName,
            request.Port,
            request.Description,
            request.IsGlobal,
            request.MaxConcurrentAssociations,
            request.TimeoutSeconds);

        await pacsRepository.AddAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server created: {PacsId} {AeTitle}", pacs.Id, request.AeTitle);
        return pacs;
    }

    public async Task<bool> EnableAsync(string id, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        pacs.Enable();
        await pacsRepository.UpdateAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server enabled: {PacsId}", id);
        return true;
    }

    public async Task<bool> DisableAsync(string id, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        pacs.Disable();
        await pacsRepository.UpdateAsync(pacs, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server disabled: {PacsId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var pacs = await pacsRepository.GetByIdAsync(id, ct);
        if (pacs is null) return false;

        await pacsRepository.DeleteAsync(id, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("PACS server deleted: {PacsId}", id);
        return true;
    }
}
