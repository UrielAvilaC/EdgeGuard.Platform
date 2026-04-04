using Dicom.Edge.Contracts.Hub;
using Dicom.Edge.Hub.Domain.Aggregates.Routing;

namespace Dicom.Edge.Hub.Application.Routing;

/// <summary>
/// Application service for Hl7RoutingRule aggregate write operations.
/// </summary>
public interface IRoutingRuleService
{
    /// <summary>Creates a new routing rule from the request DTO and persists it.</summary>
    Task<Hl7RoutingRule> CreateAsync(CreateRoutingRuleRequest request, CancellationToken ct = default);

    /// <summary>Enables a routing rule. Returns false if not found.</summary>
    Task<bool> EnableAsync(string id, CancellationToken ct = default);

    /// <summary>Disables a routing rule. Returns false if not found.</summary>
    Task<bool> DisableAsync(string id, CancellationToken ct = default);

    /// <summary>Updates the priority of a routing rule. Returns false if not found.</summary>
    Task<bool> UpdatePriorityAsync(string id, int priority, CancellationToken ct = default);

    /// <summary>Deletes a routing rule. Returns false if not found.</summary>
    Task<bool> DeleteAsync(string id, CancellationToken ct = default);
}
