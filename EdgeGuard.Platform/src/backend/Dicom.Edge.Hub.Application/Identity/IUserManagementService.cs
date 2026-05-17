using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Security.Authorization;

namespace Dicom.Edge.Hub.Application.Identity;

/// <summary>
/// User management operations (CRUD, roles, permissions, password reset).
/// </summary>
public interface IUserManagementService
{
    Task<UserDto> CreateUserAsync(CreateUserCommand command, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<UserDto>> GetFilteredPagedAsync(PaginationRequest pagination, UserFilterCriteria filter, CancellationToken ct = default);
    Task UpdateProfileAsync(string userId, string fullName, CancellationToken ct = default);
    Task DeactivateAsync(string userId, CancellationToken ct = default);
    Task ActivateAsync(string userId, CancellationToken ct = default);
    Task AssignRoleAsync(string userId, Role role, string? assignedBy = null, CancellationToken ct = default);
    Task RemoveRoleAsync(string userId, Role role, CancellationToken ct = default);
    Task GrantPermissionAsync(string userId, Permission permission, string? grantedBy = null, CancellationToken ct = default);
    Task RevokePermissionAsync(string userId, Permission permission, CancellationToken ct = default);
    Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default);
    Task UnlockAsync(string userId, CancellationToken ct = default);
}

public sealed record CreateUserCommand(
    string Username,
    string Password,
    string FullName,
    List<Role>? Roles = null,
    string? CreatedBy = null);

public sealed record UserDto(
    string Id,
    string Username,
    string FullName,
    bool IsActive,
    bool IsLocked,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> DirectPermissions);
