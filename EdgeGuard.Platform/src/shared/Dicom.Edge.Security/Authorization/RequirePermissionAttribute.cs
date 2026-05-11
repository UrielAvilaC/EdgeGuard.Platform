using Microsoft.AspNetCore.Authorization;

namespace Dicom.Edge.Security.Authorization;

/// <summary>
/// Attribute that enforces a granular <see cref="Permission"/> on a controller or action.
/// Usage: <c>[RequirePermission(Permission.ViewStudies)]</c>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Policy prefix used to build dynamic policy names.
    /// </summary>
    internal const string PolicyPrefix = "Permission_";

    public Permission Permission { get; }

    public RequirePermissionAttribute(Permission permission)
        : base($"{PolicyPrefix}{(int)permission}")
    {
        Permission = permission;
    }
}
