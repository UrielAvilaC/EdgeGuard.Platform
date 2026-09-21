using System.Reflection;

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

    /// <summary>
    /// Versión del servicio que publica <c>/api/info</c>, leída del propio ensamblado.
    ///
    /// <para>Antes era una constante escrita a mano, y por eso llevaba tiempo diciendo
    /// "1.0.0" mientras los paquetes iban por otra numeración: el número vivía sólo en el
    /// nombre del .zip, así que no había forma de preguntarle a un Hub instalado qué
    /// versión estaba corriendo.</para>
    ///
    /// <para>El origen es <c>&lt;Version&gt;</c> de <c>Directory.Build.props</c>. El SDK
    /// le añade a <c>InformationalVersion</c> el hash del commit tras un <c>+</c>
    /// (<c>1.4.1+a1b2c3…</c>); aquí se recorta porque este valor es para mostrar. El
    /// sufijo sigue disponible en las propiedades del archivo para quien lo necesite.</para>
    /// </summary>
    public static readonly string ServiceVersion = ResolveServiceVersion();

    private static string ResolveServiceVersion()
    {
        var informational = typeof(HubApiConstants).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus > 0 ? informational[..plus] : informational;
        }

        // Sin el atributo, la versión del ensamblado sirve igual. El "0.0.0" final sólo
        // aparecería en un ensamblado sin ninguna versión, que el SDK no produce.
        return typeof(HubApiConstants).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    // ==================== Bootstrap & Configuration ====================

    /// <summary>File path for Hub bootstrap log output during startup.</summary>
    public const string BootstrapLogPath = "logs/hub-bootstrap-.log";

    /// <summary>Connection string key for the Hub PostgreSQL database.</summary>
    public const string ConnectionStringName = "HubDatabase";

    /// <summary>
    /// Environment variable for the Hub PostgreSQL connection string.
    /// Takes precedence over appsettings.json.
    /// </summary>
    public const string ConnectionStringEnvVar = "EDGEGUARD_HUB_CONNECTIONSTRING";

    /// <summary>Error message when the required database connection string is missing.</summary>
    public const string MissingConnectionStringMessage =
        "Set EDGEGUARD_HUB_CONNECTIONSTRING environment variable or configure ConnectionStrings:HubDatabase.";

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
