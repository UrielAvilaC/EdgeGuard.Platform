namespace Dicom.Edge.Security.Authorization;

/// <summary>
/// Policy name constants derived from <see cref="Permission"/> values.
/// Uses the same "Permission_{id}" naming convention as <see cref="RequirePermissionAttribute"/>
/// so that <see cref="PermissionPolicyProvider"/> resolves them dynamically.
/// Usage: <c>[Authorize(Policy = Policies.ViewStudies)]</c>
/// </summary>
public static class Policies
{
    private const string P = "Permission_";

    // ── Studies (1-7) ────────────────────────────────────────────────────
    public const string ViewStudies      = P + "1";
    public const string SendStudies      = P + "2";
    public const string DeleteStudies    = P + "3";
    public const string ExportStudies    = P + "5";
    public const string EditStudyMetadata = P + "6";

    // ── Queue (10-13) ────────────────────────────────────────────────────
    public const string ViewQueue   = P + "10";
    public const string ManageQueue = P + "11";

    // ── Configuration (20-25) ────────────────────────────────────────────
    public const string ViewConfiguration  = P + "20";
    public const string EditConfiguration  = P + "21";
    public const string ManageRoutingRules = P + "23";

    // ── Monitoring (30-34) ───────────────────────────────────────────────
    public const string ViewMetrics      = P + "30";
    public const string ViewAuditLogs    = P + "31";
    public const string ViewSystemStatus = P + "32";
    public const string ExportReports    = P + "33";

    // ── Users (40-43) ────────────────────────────────────────────────────
    public const string ViewUsers        = P + "40";
    public const string ManageUsers      = P + "41";
    public const string ManageRoles      = P + "42";
    public const string GrantPermissions = P + "43";

    // ── Nodes (50-53) ────────────────────────────────────────────────────
    public const string ViewNodes       = P + "50";
    public const string ManageEdgeNodes = P + "51";

    // ── System Admin (60-64) ─────────────────────────────────────────────
    public const string ViewSystemLogs    = P + "62";
    public const string SystemMaintenance = P + "63";

    // ── Security (80-84) ─────────────────────────────────────────────────
    public const string ViewSecurityEvents = P + "80";

    // ── API (90-92) ──────────────────────────────────────────────────────
    public const string ApiAccess          = P + "90";
    public const string ManageIntegrations = P + "92";
}
