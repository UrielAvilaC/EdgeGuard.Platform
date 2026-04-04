namespace Dicom.Edge.Diagnostics.Constants;

/// <summary>
/// Constants for the bootstrap logger used during application startup
/// before the full DI container is available.
/// </summary>
public static class BootstrapConstants
{
    /// <summary>Default file path for bootstrap log output.</summary>
    public const string DefaultLogFilePath = "logs/bootstrap-.log";

    /// <summary>Default fatal shutdown message when the application terminates unexpectedly.</summary>
    public const string FatalShutdownMessage = "Application terminated unexpectedly";

    /// <summary>Console output template tag for bootstrap log events.</summary>
    public const string BootstrapTag = "BOOTSTRAP";

    /// <summary>Console output template used during bootstrap.</summary>
    public const string ConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [BOOTSTRAP] {Message:lj}{NewLine}{Exception}";

    /// <summary>File output template used during bootstrap.</summary>
    public const string FileOutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [BOOTSTRAP] {Message:lj}{NewLine}{Exception}";
}
