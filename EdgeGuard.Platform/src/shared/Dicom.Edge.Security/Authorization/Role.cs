namespace Dicom.Edge.Security.Authorization
{
    /// <summary>
    /// Predefined roles in the EdgeGuard system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Roles represent job functions and are assigned permissions based on the principle
    /// of least privilege. Custom roles can be created programmatically.
    /// </para>
    /// </remarks>
    public enum Role
    {
        /// <summary>
        /// System administrator with full access to all features.
        /// </summary>
        /// <remarks>
        /// Permissions: All permissions granted.
        /// </remarks>
        Admin = 0,

        /// <summary>
        /// Operator responsible for day-to-day system operations.
        /// </summary>
        /// <remarks>
        /// Permissions: View/send studies, manage queue, view metrics, basic configuration.
        /// </remarks>
        Operator = 1,

        /// <summary>
        /// Read-only viewer for studies and reports.
        /// </summary>
        /// <remarks>
        /// Permissions: View studies, view metrics, view reports.
        /// </remarks>
        Viewer = 2,

        /// <summary>
        /// Technical staff managing modalities and system configuration.
        /// </summary>
        /// <remarks>
        /// Permissions: View/send studies, manage modalities, manage routing, restart nodes.
        /// </remarks>
        Technician = 3,

        /// <summary>
        /// Auditor with access to logs and compliance reports.
        /// </summary>
        /// <remarks>
        /// Permissions: View audit logs, view security events, export compliance reports.
        /// </remarks>
        Auditor = 4,

        /// <summary>
        /// Manager with administrative and reporting capabilities.
        /// </summary>
        /// <remarks>
        /// Permissions: All Operator permissions plus user management and advanced reporting.
        /// </remarks>
        Manager = 5,

        /// <summary>
        /// Service account for automated processes and integrations.
        /// </summary>
        /// <remarks>
        /// Permissions: API access, limited operational permissions.
        /// </remarks>
        ServiceAccount = 6,

        /// <summary>
        /// Support personnel with diagnostic and troubleshooting access.
        /// </summary>
        /// <remarks>
        /// Permissions: View system status, view logs, system maintenance.
        /// </remarks>
        Support = 7
    }
}
