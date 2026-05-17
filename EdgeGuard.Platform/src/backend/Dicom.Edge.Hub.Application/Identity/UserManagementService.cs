using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Common.Filters;
using Dicom.Edge.Common.Pagination;
using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Dicom.Edge.Security.Authorization;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Identity;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ILogger<UserManagementService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserCommand command, CancellationToken ct = default)
    {
        var policyError = PasswordPolicy.Validate(command.Password);
        if (policyError is not null)
            throw new InvalidOperationException(policyError);

        if (await _userRepository.ExistsUsernameAsync(command.Username, ct))
            throw new InvalidOperationException($"Username '{command.Username}' already exists.");

        var hash = _passwordHasher.HashPassword(command.Password);
        var user = User.Create(command.Username, hash, command.FullName, command.CreatedBy);

        if (command.Roles is not null)
        {
            foreach (var role in command.Roles)
                user.AssignRole(role, command.CreatedBy);
        }

        await _userRepository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User '{Username}' created by '{CreatedBy}'", user.Username, command.CreatedBy);

        return MapToDto(user);
    }

    public async Task<UserDto?> GetByIdAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetWithAllRelationsAsync(userId, ct);
        return user is null ? null : MapToDto(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        return users.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<UserDto>> GetFilteredPagedAsync(PaginationRequest pagination, UserFilterCriteria filter, CancellationToken ct = default)
    {
        var result = await _userRepository.GetFilteredPagedAsync(pagination, filter, ct);
        return new PagedResult<UserDto>
        {
            Items = result.Items.Select(MapToDto).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task UpdateProfileAsync(string userId, string fullName, CancellationToken ct = default)
    {
        var user = await GetRequiredUserAsync(userId, ct);
        user.UpdateProfile(fullName);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(string userId, CancellationToken ct = default)
    {
        var user = await GetRequiredUserAsync(userId, ct);
        user.Deactivate();
        user.RevokeAllSessions();
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("User '{UserId}' deactivated", userId);
    }

    public async Task ActivateAsync(string userId, CancellationToken ct = default)
    {
        var user = await GetRequiredUserAsync(userId, ct);
        user.Activate();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task AssignRoleAsync(string userId, Role role, string? assignedBy = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetWithRolesAsync(userId, ct)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");
        user.AssignRole(role, assignedBy);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Role '{Role}' assigned to user '{UserId}'", role, userId);
    }

    public async Task RemoveRoleAsync(string userId, Role role, CancellationToken ct = default)
    {
        var user = await _userRepository.GetWithRolesAsync(userId, ct)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");
        user.RemoveRole(role);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task GrantPermissionAsync(string userId, Permission permission, string? grantedBy = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetWithAllRelationsAsync(userId, ct)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");
        user.GrantPermission(permission, grantedBy);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RevokePermissionAsync(string userId, Permission permission, CancellationToken ct = default)
    {
        var user = await _userRepository.GetWithAllRelationsAsync(userId, ct)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");
        user.RevokePermission(permission);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(string userId, string newPassword, CancellationToken ct = default)
    {
        var policyError = PasswordPolicy.Validate(newPassword);
        if (policyError is not null)
            throw new InvalidOperationException(policyError);

        var user = await GetRequiredUserAsync(userId, ct);
        var hash = _passwordHasher.HashPassword(newPassword);
        user.ChangePassword(hash);
        user.RevokeAllSessions();
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Password reset for user '{UserId}'", userId);
    }

    public async Task UnlockAsync(string userId, CancellationToken ct = default)
    {
        var user = await GetRequiredUserAsync(userId, ct);
        user.Unlock();
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("User '{UserId}' unlocked", userId);
    }

    private async Task<User> GetRequiredUserAsync(string userId, CancellationToken ct) =>
        await _userRepository.GetWithAllRelationsAsync(userId, ct)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        user.IsActive,
        user.IsLocked,
        user.LastLoginAt,
        user.CreatedAt,
        user.Roles.Select(r => r.Role.ToString()).ToList(),
        user.DirectPermissions.Where(p => p.IsGranted).Select(p => p.Permission.ToString()).ToList());
}
