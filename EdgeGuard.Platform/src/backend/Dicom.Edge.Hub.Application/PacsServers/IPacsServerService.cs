using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Pacs;

namespace Dicom.Edge.Hub.Application.PacsServers;

/// <summary>
/// Application service for PacsServer aggregate write operations.
/// </summary>
public interface IPacsServerService
{
    /// <summary>Creates a new PACS server from the request DTO and persists it.</summary>
    Task<PacsServer> CreateAsync(CreatePacsServerRequest request, CancellationToken ct = default);

    /// <summary>Updates PACS server configuration. Returns false if not found.</summary>
    Task<bool> UpdateAsync(string id, UpdatePacsServerRequest request, CancellationToken ct = default);

    /// <summary>Enables a PACS server. Returns false if not found.</summary>
    Task<bool> EnableAsync(string id, CancellationToken ct = default);

    /// <summary>Disables a PACS server. Returns false if not found.</summary>
    Task<bool> DisableAsync(string id, CancellationToken ct = default);

    /// <summary>Deletes a PACS server. Returns false if not found.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
