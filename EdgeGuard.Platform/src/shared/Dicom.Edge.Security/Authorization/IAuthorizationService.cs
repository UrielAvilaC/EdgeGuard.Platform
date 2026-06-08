using System.Security.Claims;

namespace Dicom.Edge.Security.Authorization
{
    /// <summary>
    /// Service for authorization and permission checking.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Checks if a user has a specific permission.
        /// </summary>
        /// <param name="user">User claims principal.</param>
        /// <param name="permission">Permission to check.</param>
        /// <returns>True if user has permission; otherwise false.</returns>
        Task<bool> HasPermissionAsync(ClaimsPrincipal user, Permission permission);

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        /// <param name="user">User claims principal.</param>
        /// <param name="role">Role to check.</param>
        /// <returns>True if user has role; otherwise false.</returns>
        Task<bool> HasRoleAsync(ClaimsPrincipal user, Role role);

        /// <summary>
        /// Authorizes an action on a resource.
        /// </summary>
        /// <param name="user">User claims principal.</param>
        /// <param name="resource">Resource being accessed.</param>
        /// <param name="action">Action being performed.</param>
        /// <returns>Authorization result.</returns>
        Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string resource, string action);

        /// <summary>
        /// Gets all permissions for a user.
        /// </summary>
        /// <param name="user">User claims principal.</param>
        /// <returns>List of permissions.</returns>
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(ClaimsPrincipal user);

        /// <summary>
        /// Authorizes resource-specific access.
        /// </summary>
        /// <param name="user">User claims principal.</param>
        /// <param name="resource">Resource object.</param>
        /// <param name="operation">Operation to perform.</param>
        /// <returns>Authorization result.</returns>
        Task<AuthorizationResult> AuthorizeResourceAsync(ClaimsPrincipal user, object resource, string operation);
    }
}
