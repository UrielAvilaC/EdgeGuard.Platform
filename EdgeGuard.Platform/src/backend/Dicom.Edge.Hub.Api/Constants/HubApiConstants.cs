namespace Dicom.Edge.Hub.Api.Constants;

/// <summary>
/// Centralized string constants for the Hub API layer including service identity,
/// connection configuration, endpoint messages, and capability descriptors.
/// </summary>
public static class HubApiConstants
{
    // ==================== Service Identity ====================

    /// <summary>Technical service name used in runtime diagnostics.</summary>
    public const string ServiceName = "Dicom.Edge.Hub.Api";

    /// <summary>Human-readable service display name used in /info and registration.</summary>
    public const string ServiceDisplayName = "EdgeGuard.Hub";

    /// <summary>Current service version exposed via /info endpoint.</summary>
    public const string ServiceVersion = "1.0.0";

    // ==================== Bootstrap & Configuration ====================

    /// <summary>File path for Hub bootstrap log output during startup.</summary>
    public const string BootstrapLogPath = "logs/hub-bootstrap-.log";

    /// <summary>Connection string key for the Hub PostgreSQL database.</summary>
    public const string ConnectionStringName = "HubDatabase";

    /// <summary>Error message when the required database connection string is missing.</summary>
    public const string MissingConnectionStringMessage =
        "ConnectionStrings:HubDatabase is required. Configure a PostgreSQL connection string.";

    /// <summary>Environment variable name for ASP.NET Core hosting environment.</summary>
    public const string EnvironmentVariableName = "ASPNETCORE_ENVIRONMENT";

    /// <summary>Default environment when the environment variable is not set.</summary>
    public const string DefaultEnvironment = "Production";

    // ==================== Capabilities ====================

    /// <summary>Array of capabilities reported by the /info endpoint.</summary>
    public static readonly string[] Capabilities =
        ["hl7-receive", "hl7-dispatch", "node-management", "study-tracking"];

    // ==================== Registration & Response Messages ====================

    /// <summary>Response message when a node re-registers successfully.</summary>
    public const string ReRegisteredMessage = "Re-registered successfully.";

    /// <summary>Response message when a node registers for the first time.</summary>
    public const string RegisteredMessage = "Registered successfully.";

    /// <summary>Format template for node-not-registered errors. {0} = NodeId.</summary>
    public const string NodeNotRegisteredTemplate = "Node {0} is not registered.";

    /// <summary>Validation message when a patient name search query is missing.</summary>
    public const string NameQueryRequired = "Name query is required.";

    /// <summary>Response message after successfully seeding default settings.</summary>
    public const string DefaultsSeededMessage = "Defaults seeded successfully.";
}
