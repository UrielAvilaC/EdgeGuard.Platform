using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Dicom.Edge.Security.Authorization
{
    /// <summary>
    /// Enterprise authorization service with policy-based permissions.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> _logger;

        // Permission matrix mapping roles to permissions
        private static readonly Dictionary<Role, HashSet<Permission>> _rolePermissions = new()
        {
            [Role.Admin] = new HashSet<Permission>(Enum.GetValues<Permission>()),

            [Role.Manager] = new HashSet<Permission>
            {
                Permission.ViewStudies,
                Permission.SendStudies,
                Permission.ExportStudies,
                Permission.ViewQueue,
                Permission.ManageQueue,
                Permission.RetryTransfers,
                Permission.ViewConfiguration,
                Permission.ManageModalities,
                Permission.ManageRoutingRules,
                Permission.ViewMetrics,
                Permission.ViewAuditLogs,
                Permission.ViewSystemStatus,
                Permission.ExportReports,
                Permission.ViewUsers,
                Permission.ManageUsers,
                Permission.ManageRoles,
                Permission.ViewNodes,
                Permission.ManageEdgeNodes
            },

            [Role.Operator] = new HashSet<Permission>
            {
                Permission.ViewStudies,
                Permission.SendStudies,
                Permission.ViewQueue,
                Permission.ManageQueue,
                Permission.RetryTransfers,
                Permission.ViewConfiguration,
                Permission.ViewMetrics,
                Permission.ViewSystemStatus,
                Permission.ViewNodes
            },

            [Role.Technician] = new HashSet<Permission>
            {
                Permission.ViewStudies,
                Permission.SendStudies,
                Permission.ViewQueue,
                Permission.RetryTransfers,
                Permission.ManageModalities,
                Permission.ManageRoutingRules,
                Permission.ViewMetrics,
                Permission.ViewNodes,
                Permission.RestartNodes
            },

            [Role.Viewer] = new HashSet<Permission>
            {
                Permission.ViewStudies,
                Permission.ViewMetrics,
                Permission.ViewSystemStatus
            },

            [Role.Auditor] = new HashSet<Permission>
            {
                Permission.ViewAuditLogs,
                Permission.ViewSecurityEvents,
                Permission.ExportReports,
                Permission.ExportComplianceReports,
                Permission.SecurityAudit
            },

            [Role.Support] = new HashSet<Permission>
            {
                Permission.ViewSystemStatus,
                Permission.ViewSystemLogs,
                Permission.ViewMetrics,
                Permission.ViewNodes,
                Permission.SystemMaintenance
            },

            [Role.ServiceAccount] = new HashSet<Permission>
            {
                Permission.ApiAccess,
                Permission.ViewStudies,
                Permission.SendStudies
            }
        };

        public AuthorizationService(ILogger<AuthorizationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal user, Permission permission)
        {
            if (user == null)
                return false;

            var roles = GetUserRoles(user);

            // Check role-based permissions
            foreach (var role in roles)
            {
                if (_rolePermissions.TryGetValue(role, out var permissions) &&
                    permissions.Contains(permission))
                {
                    return true;
                }
            }

            // Could extend here to check user-specific permissions from database

            return false;
        }

        public Task<bool> HasRoleAsync(ClaimsPrincipal user, Role role)
        {
            if (user == null)
                return Task.FromResult(false);

            var roles = GetUserRoles(user);
            return Task.FromResult(roles.Contains(role));
        }

        public async Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            string resource,
            string action)
        {
            if (user == null)
                return AuthorizationResult.Failed("User is not authenticated");

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var roles = GetUserRoles(user);

            if (!roles.Any())
            {
                _logger.LogWarning("User {UserId} has no roles assigned", userId);
                LogAuthorization(userId, resource, action, false, "No roles assigned");
                return AuthorizationResult.Failed("User has no roles assigned");
            }

            // Admin bypass
            if (roles.Contains(Role.Admin))
            {
                LogAuthorization(userId, resource, action, true, "Admin bypass");
                return AuthorizationResult.Success();
            }

            // Check resource-specific permissions
            var hasPermission = await CheckResourcePermissionAsync(user, resource, action, roles);

            LogAuthorization(userId, resource, action, hasPermission);

            return hasPermission
                ? AuthorizationResult.Success()
                : AuthorizationResult.Failed($"User lacks permission for {action} on {resource}");
        }

        public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(ClaimsPrincipal user)
        {
            if (user == null)
                return Enumerable.Empty<Permission>();

            var roles = GetUserRoles(user);
            var permissions = new HashSet<Permission>();

            foreach (var role in roles)
            {
                if (_rolePermissions.TryGetValue(role, out var rolePerms))
                {
                    permissions.UnionWith(rolePerms);
                }
            }

            // Could extend to add user-specific permissions from database

            return permissions;
        }

        public async Task<AuthorizationResult> AuthorizeResourceAsync(
            ClaimsPrincipal user,
            object resource,
            string operation)
        {
            if (user == null)
                return AuthorizationResult.Failed("User is not authenticated");

            if (resource == null)
                return AuthorizationResult.Failed("Resource cannot be null");

            // Resource-based authorization would go here
            // For now, delegate to basic authorization
            var resourceType = resource.GetType().Name;
            return await AuthorizeAsync(user, resourceType, operation);
        }

        [Obsolete("Use HasPermissionAsync(ClaimsPrincipal, Permission) instead")]
        public bool HasPermission(Role role, Permission permission)
        {
            if (_rolePermissions.TryGetValue(role, out var permissions))
            {
                return permissions.Contains(permission);
            }
            return false;
        }

        private List<Role> GetUserRoles(ClaimsPrincipal user)
        {
            return user.FindAll(ClaimTypes.Role)
                .Select(c => Enum.TryParse<Role>(c.Value, out var role) ? role : (Role?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .ToList();
        }

        private async Task<bool> CheckResourcePermissionAsync(
            ClaimsPrincipal user,
            string resource,
            string action,
            List<Role> roles)
        {
            // Map resource + action to required permission
            var requiredPermission = (resource.ToLowerInvariant(), action.ToLowerInvariant()) switch
            {
                // Studies
                ("study", "view") => Permission.ViewStudies,
                ("study", "send") => Permission.SendStudies,
                ("study", "delete") => Permission.DeleteStudies,
                ("study", "archive") => Permission.ArchiveStudies,
                ("study", "export") => Permission.ExportStudies,
                ("study", "edit") => Permission.EditStudyMetadata,

                // Queue
                ("queue", "view") => Permission.ViewQueue,
                ("queue", "manage") => Permission.ManageQueue,
                ("queue", "retry") => Permission.RetryTransfers,

                // Configuration
                ("configuration", "view") => Permission.ViewConfiguration,
                ("configuration", "edit") => Permission.EditConfiguration,
                ("modality", "manage") => Permission.ManageModalities,
                ("routing", "manage") => Permission.ManageRoutingRules,

                // Metrics & Monitoring
                ("metrics", "view") => Permission.ViewMetrics,
                ("auditlog", "view") => Permission.ViewAuditLogs,
                ("system", "view") => Permission.ViewSystemStatus,

                // Users
                ("user", "view") => Permission.ViewUsers,
                ("user", "manage") => Permission.ManageUsers,

                // Nodes
                ("node", "view") => Permission.ViewNodes,
                ("node", "manage") => Permission.ManageEdgeNodes,
                ("node", "restart") => Permission.RestartNodes,

                _ => (Permission?)null
            };

            if (requiredPermission == null)
            {
                _logger.LogWarning("Unknown resource/action combination: {Resource}/{Action}", resource, action);
                return false;
            }

            return await HasPermissionAsync(user, requiredPermission.Value);
        }

        private void LogAuthorization(
            string? userId,
            string resource,
            string action,
            bool granted,
            string? reason = null)
        {
            var level = granted ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(level,
                "Authorization {Result} for user {UserId} on {Resource}/{Action}{Reason}",
                granted ? "granted" : "denied",
                userId ?? "unknown",
                resource,
                action,
                reason != null ? $" ({reason})" : "");
        }
    }
}
