using Dicom.Edge.Security.Authorization;

namespace Dicom.Edge.Security.Repositories
{
    /// <summary>
    /// Repository for managing user permissions.
    /// </summary>
    public interface IPermissionRepository
    {
        /// <summary>
        /// Gets all permissions assigned to a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of permissions.</returns>
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all permissions assigned to a role.
        /// </summary>
        /// <param name="role">Role.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of permissions.</returns>
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Grants a permission to a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="permission">Permission to grant.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task GrantPermissionAsync(string userId, Permission permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a permission from a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="permission">Permission to revoke.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RevokePermissionAsync(string userId, Permission permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user has a specific permission.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="permission">Permission to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if user has permission; otherwise false.</returns>
        Task<bool> HasPermissionAsync(string userId, Permission permission, CancellationToken cancellationToken = default);
    }
}
