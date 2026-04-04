using Serilog;
using Serilog.Events;

namespace Dicom.Edge.Diagnostics.Bootstrap;

/// <summary>
/// Provides a bootstrap logger that captures log events during application startup,
/// before the DI container and full Serilog pipeline are configured.
/// Critical for diagnosing configuration errors, missing dependencies, and
/// crash-on-start scenarios.
/// </summary>
public static class BootstrapLogger
{
    /// <summary>
    /// Initializes the Serilog bootstrap logger with a minimal file + console configuration.
    /// Must be called before any other logging or host setup.
    /// </summary>
    public static void Initialize(string logFilePath = "logs/bootstrap-.log")
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(logFilePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [BOOTSTRAP] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [BOOTSTRAP] {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();
    }

    /// <summary>
    /// Logs a fatal error during startup and ensures the event is flushed to disk.
    /// </summary>
    public static void FatalShutdown(Exception exception, string message = "Application terminated unexpectedly")
    {
        Log.Fatal(exception, message);
    }

    /// <summary>
    /// Flushes all pending log events and closes the logger.
    /// Must be called in a finally block at the end of Program.cs.
    /// </summary>
    public static void CloseAndFlush()
    {
        Log.CloseAndFlush();
    }
}
