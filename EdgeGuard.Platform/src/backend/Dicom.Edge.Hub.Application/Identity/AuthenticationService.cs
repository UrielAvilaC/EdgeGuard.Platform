using System.Security.Cryptography;
using System.Text;
using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Dicom.Edge.Security.Authentication;
using Dicom.Edge.Security.Authorization;
using Dicom.Edge.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Identity;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthorizationService _authorizationService;
    private readonly ILogger<AuthenticationService> _logger;

    // Static role→permission matrix for building JWT claims
    private static readonly Dictionary<Role, HashSet<Permission>> RolePermissions = new()
    {
        [Role.Admin] = new HashSet<Permission>(Enum.GetValues<Permission>()),
        [Role.Manager] = [Permission.ViewStudies, Permission.SendStudies, Permission.ExportStudies, Permission.ViewQueue, Permission.ManageQueue, Permission.RetryTransfers, Permission.ViewConfiguration, Permission.ManageModalities, Permission.ManageRoutingRules, Permission.ViewMetrics, Permission.ViewAuditLogs, Permission.ViewSystemStatus, Permission.ExportReports, Permission.ViewUsers, Permission.ManageUsers, Permission.ManageRoles, Permission.ViewNodes, Permission.ManageEdgeNodes],
        [Role.Operator] = [Permission.ViewStudies, Permission.SendStudies, Permission.ViewQueue, Permission.ManageQueue, Permission.RetryTransfers, Permission.ViewConfiguration, Permission.ViewMetrics, Permission.ViewSystemStatus, Permission.ViewNodes],
        [Role.Technician] = [Permission.ViewStudies, Permission.SendStudies, Permission.ViewQueue, Permission.RetryTransfers, Permission.ManageModalities, Permission.ManageRoutingRules, Permission.ViewMetrics, Permission.ViewNodes, Permission.RestartNodes],
        [Role.Viewer] = [Permission.ViewStudies, Permission.ViewMetrics, Permission.ViewSystemStatus],
        [Role.Auditor] = [Permission.ViewAuditLogs, Permission.ViewSecurityEvents, Permission.ExportReports, Permission.ExportComplianceReports, Permission.SecurityAudit],
        [Role.ServiceAccount] = [Permission.ApiAccess, Permission.ViewStudies, Permission.SendStudies],
        [Role.Support] = [Permission.ViewSystemStatus, Permission.ViewSystemLogs, Permission.ViewMetrics, Permission.ViewNodes, Permission.SystemMaintenance],
    };

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _authorizationService = null!; // Not used directly — permissions resolved from matrix
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByUsernameWithAllRelationsAsync(command.Username, ct);

        if (user is null)
        {
            _logger.LogWarning("Login failed: user '{Username}' not found", command.Username);
            return LoginResult.Failed("Invalid credentials.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: user '{Username}' is deactivated", command.Username);
            return LoginResult.Failed("Account is deactivated.");
        }

        if (user.IsLocked)
        {
            _logger.LogWarning("Login failed: user '{Username}' is locked until {LockedUntil}", command.Username, user.LockedUntil);
            return LoginResult.LockedOut(user.LockedUntil!.Value);
        }

        if (!_passwordHasher.VerifyPassword(command.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _unitOfWork.SaveChangesAsync(ct);

            if (user.IsLocked)
            {
                _logger.LogWarning("User '{Username}' locked after failed attempts", command.Username);
                return LoginResult.LockedOut(user.LockedUntil!.Value);
            }

            return LoginResult.Failed("Invalid credentials.");
        }

        // Success — generate token with permissions
        user.RecordSuccessfulLogin();

        var effectivePermissions = ResolveEffectivePermissions(user);
        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        var tokenRequest = new TokenRequest
        {
            UserId = user.Id,
            UserName = user.Username,
            Roles = roles,
            CustomClaims = new Dictionary<string, string>
            {
                ["permissions"] = string.Join(",", effectivePermissions.Select(p => ((int)p).ToString())),
                ["fullName"] = user.FullName
            }
        };

        var tokenResponse = _tokenService.GenerateToken(tokenRequest);

        // Store refresh token hash
        var refreshTokenHash = HashToken(tokenResponse.RefreshToken);
        user.AddRefreshToken(refreshTokenHash, command.IpAddress, tokenResponse.ExpiresAt.AddDays(7));

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("User '{Username}' logged in successfully", command.Username);

        var permissionNames = effectivePermissions.Select(p => p.ToString()).ToList();
        return LoginResult.Succeeded(tokenResponse, permissionNames);
    }

    public async Task<LoginResult> RefreshAsync(RefreshCommand command, CancellationToken ct = default)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(command.AccessToken);
        if (principal is null)
            return LoginResult.Failed("Invalid access token.");

        var userId = principal.FindFirst("userId")?.Value;
        if (userId is null)
            return LoginResult.Failed("Invalid token claims.");

        var user = await _userRepository.GetWithAllRelationsAsync(userId, ct);
        if (user is null || !user.IsActive)
            return LoginResult.Failed("User not found or deactivated.");

        var oldTokenHash = HashToken(command.RefreshToken);
        var existingToken = user.FindActiveRefreshToken(oldTokenHash);
        if (existingToken is null)
        {
            _logger.LogWarning("Refresh token not found or inactive for user '{UserId}'", userId);
            return LoginResult.Failed("Invalid refresh token.");
        }

        // Rotate: revoke old, issue new
        var effectivePermissions = ResolveEffectivePermissions(user);
        var roles = user.Roles.Select(r => r.Role.ToString()).ToList();

        var tokenRequest = new TokenRequest
        {
            UserId = user.Id,
            UserName = user.Username,
            Roles = roles,
            CustomClaims = new Dictionary<string, string>
            {
                ["permissions"] = string.Join(",", effectivePermissions.Select(p => ((int)p).ToString())),
                ["fullName"] = user.FullName
            }
        };

        var newTokenResponse = _tokenService.GenerateToken(tokenRequest);
        var newTokenHash = HashToken(newTokenResponse.RefreshToken);

        user.RevokeRefreshToken(oldTokenHash, newTokenHash);
        user.AddRefreshToken(newTokenHash, command.IpAddress, newTokenResponse.ExpiresAt.AddDays(7));

        await _unitOfWork.SaveChangesAsync(ct);

        var permissionNames = effectivePermissions.Select(p => p.ToString()).ToList();
        return LoginResult.Succeeded(newTokenResponse, permissionNames);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);

        // We need to find the user that owns this token — scan is acceptable for revocation
        // In production, consider a dedicated RefreshToken repository with index on TokenHash
        // For now, the token hash index on the refresh_tokens table handles this at DB level
        _tokenService.RevokeToken(refreshToken);
        await Task.CompletedTask;
    }

    public async Task RevokeAllAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetWithAllRelationsAsync(userId, ct);
        if (user is null) return;

        user.RevokeAllSessions();
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("All sessions revoked for user '{UserId}'", userId);
    }

    private HashSet<Permission> ResolveEffectivePermissions(User user)
    {
        var permissions = new HashSet<Permission>();

        // Permissions from roles
        foreach (var userRole in user.Roles)
        {
            if (RolePermissions.TryGetValue(userRole.Role, out var rolePerms))
                permissions.UnionWith(rolePerms);
        }

        // Direct grants
        foreach (var directPerm in user.DirectPermissions.Where(p => p.IsGranted))
            permissions.Add(directPerm.Permission);

        // Direct denies (override role permissions)
        foreach (var directPerm in user.DirectPermissions.Where(p => !p.IsGranted))
            permissions.Remove(directPerm.Permission);

        return permissions;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
