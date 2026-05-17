using Dicom.Edge.Hub.Domain.Aggregates.Identity.Events;
using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Identity;

/// <summary>
/// User aggregate root. Manages authentication, roles, permissions, and sessions.
/// </summary>
public sealed class User : AggregateRoot<string>
{
    private readonly List<UserRole> _roles = [];
    private readonly List<UserPermission> _directPermissions = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private const int MaxActiveRefreshTokens = 5;

    public string Username { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public DateTime PasswordChangedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? CreatedBy { get; private set; }

    public IReadOnlyList<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyList<UserPermission> DirectPermissions => _directPermissions.AsReadOnly();
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    private User() { }

    public static User Create(
        string username,
        string passwordHash,
        string fullName,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        var user = new User
        {
            Id = IdGenerator.NewId(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            IsActive = true,
            FailedLoginAttempts = 0,
            PasswordChangedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        user.AddDomainEvent(new UserCreatedEvent(user.Id, user.Username));

        return user;
    }

    /// <summary>
    /// Records a successful login. Resets failed attempts and updates last login.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        LastLoginAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new UserLoginEvent(Id, Username, true));
    }

    /// <summary>
    /// Records a failed login attempt. Locks account after <see cref="MaxFailedAttempts"/>.
    /// </summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        UpdatedAt = DateTime.UtcNow;

        if (FailedLoginAttempts >= MaxFailedAttempts)
        {
            LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
            AddDomainEvent(new UserLockedEvent(Id, Username, LockedUntil.Value));
        }

        AddDomainEvent(new UserLoginEvent(Id, Username, false));
    }

    public void Lock(TimeSpan duration)
    {
        LockedUntil = DateTime.UtcNow.Add(duration);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new UserLockedEvent(Id, Username, LockedUntil.Value));
    }

    public void Unlock()
    {
        LockedUntil = null;
        FailedLoginAttempts = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        PasswordHash = newPasswordHash;
        PasswordChangedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PasswordChangedEvent(Id, Username));
    }

    public void UpdateProfile(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        FullName = fullName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Role Management ──────────────────────────────────────────

    public void AssignRole(Security.Authorization.Role role, string? assignedBy = null)
    {
        if (_roles.Any(r => r.Role == role))
            return;

        _roles.Add(UserRole.Create(Id, role, assignedBy));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveRole(Security.Authorization.Role role)
    {
        var existing = _roles.FirstOrDefault(r => r.Role == role);
        if (existing is not null)
        {
            _roles.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    // ── Direct Permission Management ─────────────────────────────

    public void GrantPermission(Security.Authorization.Permission permission, string? grantedBy = null)
    {
        var existing = _directPermissions.FirstOrDefault(p => p.Permission == permission);
        if (existing is not null)
        {
            if (existing.IsGranted) return;
            _directPermissions.Remove(existing);
        }

        _directPermissions.Add(UserPermission.Create(Id, permission, isGranted: true, grantedBy));
        UpdatedAt = DateTime.UtcNow;
    }

    public void DenyPermission(Security.Authorization.Permission permission, string? grantedBy = null)
    {
        var existing = _directPermissions.FirstOrDefault(p => p.Permission == permission);
        if (existing is not null)
            _directPermissions.Remove(existing);

        _directPermissions.Add(UserPermission.Create(Id, permission, isGranted: false, grantedBy));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokePermission(Security.Authorization.Permission permission)
    {
        var existing = _directPermissions.FirstOrDefault(p => p.Permission == permission);
        if (existing is not null)
        {
            _directPermissions.Remove(existing);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    // ── Refresh Token Management ─────────────────────────────────

    public void AddRefreshToken(string tokenHash, string? ipAddress, DateTime expiresAt)
    {
        // Enforce max active tokens — revoke oldest if limit exceeded
        var activeTokens = _refreshTokens
            .Where(t => t.IsActive)
            .OrderBy(t => t.CreatedAt)
            .ToList();

        while (activeTokens.Count >= MaxActiveRefreshTokens)
        {
            activeTokens[0].Revoke();
            activeTokens.RemoveAt(0);
        }

        _refreshTokens.Add(RefreshToken.Create(Id, tokenHash, expiresAt, ipAddress));
        UpdatedAt = DateTime.UtcNow;
    }

    public RefreshToken? FindActiveRefreshToken(string tokenHash)
    {
        return _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash && t.IsActive);
    }

    public void RevokeRefreshToken(string tokenHash, string? replacedByHash = null)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);
        token?.Revoke(replacedByHash);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RevokeAllSessions()
    {
        foreach (var token in _refreshTokens.Where(t => t.IsActive))
            token.Revoke();

        UpdatedAt = DateTime.UtcNow;
    }
}
