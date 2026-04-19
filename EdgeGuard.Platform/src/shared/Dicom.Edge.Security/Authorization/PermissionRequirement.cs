using Microsoft.AspNetCore.Authorization;

namespace Dicom.Edge.Security.Authorization;

/// <summary>
/// Authorization requirement that demands a specific <see cref="Permission"/>.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }

    public PermissionRequirement(Permission permission) => Permission = permission;
}
