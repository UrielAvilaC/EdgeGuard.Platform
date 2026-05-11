using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Security.Authorization;

/// <summary>
/// Dynamically creates authorization policies for <see cref="RequirePermissionAttribute"/>.
/// Resolves policy names like "Permission_1" → <see cref="PermissionRequirement"/> with that permission.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permIdStr = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            if (int.TryParse(permIdStr, out var permId) && Enum.IsDefined(typeof(Permission), permId))
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement((Permission)permId))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
