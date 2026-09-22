namespace Dicom.Edge.Node.Persistence.Constants;

/// <summary>
/// IConfiguration section names that mirror the <c>SectionName</c> constants
/// defined in each Options class (e.g. <c>DicomServerOptions.SectionName</c>).
/// Centralised here so that the Persistence layer can reference them
/// without taking a dependency on the projects that own those Options.
/// </summary>
public static class ConfigSectionNames
{
    public const string NodeApi       = "NodeApi";
    public const string HubConnection = "HubConnection";
    public const string DicomServer   = "DicomServer";
    public const string PacsSender       = "PacsSender";
    public const string PacsCEcho        = "PacsCEcho";
    public const string PacsDestination  = "PacsDestination";

    /// <summary>
    /// Sección de la configuración desplegada que fija la raíz de almacenamiento DICOM.
    /// No la lee nadie en runtime —la raíz efectiva es la fila <c>storage.root_path</c>—;
    /// es el valor que <c>NodeSettingsDeploymentHydrator</c> copia a esa fila al arrancar.
    /// </summary>
    public const string NodeStorage   = "NodeStorage";
}
