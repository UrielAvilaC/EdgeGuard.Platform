using Dicom.Edge.Security.Authentication;

namespace Dicom.Edge.Hub.Application.Identity;

/// <summary>
/// Authentication service for login, refresh, and revocation operations.
/// </summary>
public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(LoginCommand command, CancellationToken ct = default);
    Task<LoginResult> RefreshAsync(RefreshCommand command, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAllAsync(string userId, CancellationToken ct = default);
}

public sealed record LoginCommand(string Username, string Password, string? IpAddress = null);

public sealed record RefreshCommand(string AccessToken, string RefreshToken, string? IpAddress = null);

public sealed record LoginResult
{
    public bool Success { get; init; }
    public TokenResponse? Token { get; init; }
    public string? FailureReason { get; init; }
    public bool IsLocked { get; init; }
    public DateTime? LockoutEnd { get; init; }
    public IReadOnlyList<string>? Permissions { get; init; }

    public static LoginResult Succeeded(TokenResponse token, IReadOnlyList<string> permissions) =>
        new() { Success = true, Token = token, Permissions = permissions };

    public static LoginResult Failed(string reason) =>
        new() { Success = false, FailureReason = reason };

    public static LoginResult LockedOut(DateTime lockoutEnd) =>
        new() { Success = false, FailureReason = "Account is locked.", IsLocked = true, LockoutEnd = lockoutEnd };
}
