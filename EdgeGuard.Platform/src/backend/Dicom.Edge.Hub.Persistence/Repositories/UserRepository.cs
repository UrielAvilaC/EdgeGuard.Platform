using System.Linq.Expressions;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Common.Sorting;
using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Security.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly HubDbContext _context;

    public UserRepository(HubDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Users.FindAsync([id], ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
        await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<User?> GetWithRolesAsync(string id, CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetWithAllRelationsAsync(string id, CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.DirectPermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByUsernameWithAllRelationsAsync(string username, CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Roles)
            .Include(u => u.DirectPermissions)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(ct);

    public async Task<bool> ExistsUsernameAsync(string username, CancellationToken ct = default) =>
        await _context.Users
            .AnyAsync(u => u.Username == username.ToLowerInvariant(), ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<User>> GetFilteredPagedAsync(PaginationRequest pagination, UserFilterCriteria filter, CancellationToken ct = default)
    {
        var query = _context.Users.Include(u => u.Roles).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(u => u.Username.Contains(filter.Search) || u.FullName.Contains(filter.Search));
        if (filter.IsActive.HasValue) query = query.Where(u => u.IsActive == filter.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(filter.Role) && Enum.TryParse<Role>(filter.Role, true, out var role))
            query = query.Where(u => u.Roles.Any(r => r.Role == role));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .ApplySort(filter.SortBy, filter.SortDir, UserSortFields, q => q.OrderBy(u => u.Username))
            .Skip(pagination.Skip).Take(pagination.PageSize).ToListAsync(ct);

        return new PagedResult<User> { Items = items, Page = pagination.Page, PageSize = pagination.PageSize, TotalCount = totalCount };
    }

    private static readonly Dictionary<string, Expression<Func<User, object?>>> UserSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        ["username"] = u => u.Username,
        ["fullName"] = u => u.FullName,
        ["isActive"] = u => u.IsActive,
        ["createdAt"] = u => u.CreatedAt,
        ["lastLoginAt"] = u => u.LastLoginAt,
    };
}
