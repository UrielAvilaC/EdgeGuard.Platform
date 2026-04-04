namespace Dicom.Edge.Diagnostics.Constants;

/// <summary>
/// Constants for structured audit logging, including category names,
/// default identities, and scope property keys.
/// </summary>
public static class AuditConstants
{
    /// <summary>Logger source context used for all audit events.</summary>
    public const string LoggerCategory = "Audit";

    /// <summary>Scope property key identifying the audit category.</summary>
    public const string AuditCategoryKey = "AuditCategory";

    /// <summary>Default user identity when no authenticated user is available.</summary>
    public const string SystemUser = "system";
}
