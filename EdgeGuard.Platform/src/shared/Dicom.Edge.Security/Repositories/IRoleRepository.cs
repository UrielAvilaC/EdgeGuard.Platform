using Dicom.Edge.Security.Authorization;

namespace Dicom.Edge.Security.Repositories
{
    /// <summary>
    /// Repository for managing user roles.
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// Gets all roles assigned to a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of roles.</returns>
        Task<IEnumerable<Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Assigns a role to a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="role">Role to assign.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AssignRoleAsync(string userId, Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a role from a user.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="role">Role to remove.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveRoleAsync(string userId, Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user has a specific role.
        /// </summary>
        /// <param name="userId">User identifier.</param>
        /// <param name="role">Role to check.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if user has role; otherwise false.</returns>
        Task<bool> HasRoleAsync(string userId, Role role, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available roles in the system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all roles.</returns>
        Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    }
}
