using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Hub.Domain.Aggregates.Routing;

public interface IHl7RoutingRuleRepository
{
    Task<Hl7RoutingRule?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IReadOnlyList<Hl7RoutingRule>> GetEnabledOrderedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Hl7RoutingRule>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<Hl7RoutingRule>> GetFilteredPagedAsync(PaginationRequest pagination, RoutingRuleFilterCriteria filter, CancellationToken ct = default);
    Task<Hl7RoutingRule> AddAsync(Hl7RoutingRule rule, CancellationToken ct = default);
    Task UpdateAsync(Hl7RoutingRule rule, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}
