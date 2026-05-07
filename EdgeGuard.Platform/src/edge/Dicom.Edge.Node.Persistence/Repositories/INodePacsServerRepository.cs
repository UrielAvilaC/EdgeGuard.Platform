using Dicom.Edge.Node.Persistence.Entities;

namespace Dicom.Edge.Node.Persistence.Repositories;

/// <summary>
/// Repository for <see cref="NodePacsServer"/> — PACS destinations pushed from the Hub.
/// </summary>
public interface INodePacsServerRepository
{
    /// <summary>Returns all enabled PACS servers ordered by priority (ascending).</summary>
    Task<IReadOnlyList<NodePacsServer>> GetAllEnabledAsync(CancellationToken ct = default);

    /// <summary>Returns all PACS servers regardless of enabled state.</summary>
    Task<IReadOnlyList<NodePacsServer>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Looks up a PACS server by its AE title.</summary>
    Task<NodePacsServer?> GetByAeTitleAsync(string aeTitle, CancellationToken ct = default);

    /// <summary>
    /// Upserts the supplied list (insert or update by ID) and removes
    /// any existing records whose ID is NOT in <paramref name="servers"/>.
    /// </summary>
    Task SyncFromHubAsync(IReadOnlyList<NodePacsServer> servers, CancellationToken ct = default);
}
