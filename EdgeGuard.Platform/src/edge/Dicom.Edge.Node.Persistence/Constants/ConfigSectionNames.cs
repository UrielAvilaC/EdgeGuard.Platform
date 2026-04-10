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
}
