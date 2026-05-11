using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Security.Authorization;

/// <summary>
/// Handles <see cref="PermissionRequirement"/> by checking the "permissions" claim in the JWT.
/// Admin role always bypasses permission checks.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(ILogger<PermissionAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return Task.CompletedTask;
        }

        // Admin bypass
        if (user.IsInRole(Role.Admin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Read permissions claim from JWT (comma-separated integer IDs)
        var permissionsClaim = user.FindFirstValue("permissions");
        if (string.IsNullOrEmpty(permissionsClaim))
        {
            _logger.LogWarning(
                "Permission denied: user {UserId} has no permissions claim. Required: {Permission}",
                user.FindFirstValue("userId"),
                requirement.Permission);
            return Task.CompletedTask;
        }

        var requiredId = ((int)requirement.Permission).ToString();
        var granted = permissionsClaim
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(requiredId);

        if (granted)
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Permission denied: user {UserId} lacks {Permission}",
                user.FindFirstValue("userId"),
                requirement.Permission);
        }

        return Task.CompletedTask;
    }
}
