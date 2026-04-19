using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Hub.Domain.Aggregates.Identity;

/// <summary>
/// Repository interface for User aggregate.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User?> GetWithRolesAsync(string id, CancellationToken ct = default);
    Task<User?> GetWithAllRelationsAsync(string id, CancellationToken ct = default);
    Task<User?> GetByUsernameWithAllRelationsAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<User>> GetFilteredPagedAsync(PaginationRequest pagination, UserFilterCriteria filter, CancellationToken ct = default);
    Task<bool> ExistsUsernameAsync(string username, CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}
