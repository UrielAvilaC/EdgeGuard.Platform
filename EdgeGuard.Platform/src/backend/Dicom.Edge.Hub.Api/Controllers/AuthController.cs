using Dicom.Edge.Hub.Application.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dicom.Edge.Hub.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(request.Username, request.Password, GetIpAddress());
        var result = await _authService.LoginAsync(command, ct);

        if (!result.Success)
        {
            if (result.IsLocked)
                return StatusCode(423, new { result.FailureReason, result.LockoutEnd });

            return Unauthorized(new { result.FailureReason });
        }

        return Ok(new
        {
            result.Token!.AccessToken,
            result.Token.RefreshToken,
            result.Token.ExpiresIn,
            result.Token.TokenType,
            result.Permissions
        });
    }

    /// <summary>
    /// Refreshes an expired access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var command = new RefreshCommand(request.AccessToken, request.RefreshToken, GetIpAddress());
        var result = await _authService.RefreshAsync(command, ct);

        if (!result.Success)
            return Unauthorized(new { result.FailureReason });

        return Ok(new
        {
            result.Token!.AccessToken,
            result.Token.RefreshToken,
            result.Token.ExpiresIn,
            result.Token.TokenType,
            result.Permissions
        });
    }

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken ct)
    {
        await _authService.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>
    /// Revokes all sessions for the current user.
    /// </summary>
    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAll(CancellationToken ct)
    {
        var userId = User.FindFirst("userId")?.Value;
        if (userId is null)
            return Unauthorized();

        await _authService.RevokeAllAsync(userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Returns the current user's profile and permissions.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst("userId")?.Value;
        var userName = User.FindFirst("userName")?.Value;
        var fullName = User.FindFirst("fullName")?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = User.FindFirst("permissions")?.Value?.Split(',') ?? [];

        return Ok(new
        {
            UserId = userId,
            UserName = userName,
            FullName = fullName,
            Roles = roles,
            Permissions = permissions
        });
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

public record LoginRequest(string Username, string Password);
public record RefreshRequest(string AccessToken, string RefreshToken);
public record RevokeRequest(string RefreshToken);
