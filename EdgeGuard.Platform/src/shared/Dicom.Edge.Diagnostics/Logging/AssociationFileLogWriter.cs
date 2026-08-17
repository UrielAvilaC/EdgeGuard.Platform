using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Correlation;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Dicom.Edge.Diagnostics.Logging;

/// <summary>
/// Writes one log file per DICOM association and routes log events to it.
/// <para>
/// Acts both as the <see cref="IAssociationLogWriter"/> used by the SCP to open/close files and
/// as a Serilog sink: every event carrying an <c>AssociationId</c> property is forwarded to the
/// file of that association, in addition to the global log. Events without a matching open file
/// are ignored.
/// </para>
/// </summary>
/// <remarks>
/// The sink is intentionally <b>synchronous</b> (never wrapped in <c>WriteTo.Async</c>) and the
/// underlying file sink is unbuffered: ordering between <see cref="Emit"/> and
/// <see cref="Close"/> must be strict, and an aborted association — the case that matters most
/// during modality homologation — must leave a complete file on disk.
/// <para>
/// No member throws. Logging failures are reported through Serilog's <see cref="SelfLog"/> and
/// never propagate into the DICOM association.
/// </para>
/// </remarks>
public sealed class AssociationFileLogWriter(IOptionsMonitor<DiagnosticsOptions> optionsMonitor)
    : IAssociationLogWriter, ILogEventSink, IDisposable
{
    private const string FoDicomSourcePrefix = "FellowOakDicom";
    private const string SourceContextProperty = "SourceContext";

    private const string OutputTemplate =
        "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly Lock _dayGate = new();

    private string _dayKey = string.Empty;
    private int _filesToday;
    private bool _disposed;

    private PerAssociationLoggingOptions Options => optionsMonitor.CurrentValue.File.PerAssociation;

    // ── IAssociationLogWriter ────────────────────────────────────────────────

    /// <inheritdoc />
    public bool Open(AssociationLogContext context)
    {
        if (_disposed) return false;

        var options = Options;
        if (!options.Enabled) return false;

        if (_entries.ContainsKey(context.AssociationId)) return true;

        try
        {
            if (_entries.Count >= options.MaxOpen)
            {
                SelfLog.WriteLine(
                    "Association log not opened for {0}: MaxOpen={1} reached",
                    context.AssociationId, options.MaxOpen);
                return false;
            }

            var directory = Path.Combine(
                Path.GetFullPath(options.Path),
                context.ConnectedAt.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (!TryReserveDailySlot(directory, options.MaxFilesPerDay, context.AssociationId))
                return false;

            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, BuildFileName(context));

            if (!options.UseCompactJson)
                File.AppendAllText(filePath, BuildHeader(context), Encoding.UTF8);

            var level = ParseLevel(options.MinimumLevel);
            var configuration = new LoggerConfiguration().MinimumLevel.Is(level);

            var sizeLimit = options.MaxFileSizeMb * 1024L * 1024L;
            _ = options.UseCompactJson
                ? configuration.WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: filePath,
                    fileSizeLimitBytes: sizeLimit,
                    rollOnFileSizeLimit: false,
                    buffered: false,
                    shared: false)
                : configuration.WriteTo.File(
                    path: filePath,
                    outputTemplate: OutputTemplate,
                    fileSizeLimitBytes: sizeLimit,
                    rollOnFileSizeLimit: false,
                    buffered: false,
                    shared: false);

            var entry = new Entry(configuration.CreateLogger(), filePath, context);

            if (!_entries.TryAdd(context.AssociationId, entry))
            {
                entry.Logger.Dispose();
                return true;
            }

            if (options.UseCompactJson)
                WriteOpenedEvent(entry, context);

            return true;
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine(
                "Failed to open association log for {0}: {1}", context.AssociationId, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public string? Close(AssociationLogContext context, AssociationSummary summary)
        => Close(context.AssociationId, summary);

    private string? Close(string associationId, AssociationSummary summary)
    {
        if (!_entries.TryRemove(associationId, out var entry))
            return null;

        var useJson = Options.UseCompactJson;

        try
        {
            if (useJson)
                WriteClosedEvent(entry, summary);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Failed to write association footer for {0}: {1}", associationId, ex);
        }

        // Dispose flushes and releases the file handle; the plain-text footer is appended
        // afterwards so it cannot race with the sink's own writes.
        entry.Logger.Dispose();

        if (useJson) return entry.FilePath;

        try
        {
            File.AppendAllText(entry.FilePath, BuildFooter(entry, summary), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Failed to append association footer for {0}: {1}", associationId, ex);
        }

        return entry.FilePath;
    }

    /// <inheritdoc />
    public int SweepStale(TimeSpan ttl)
    {
        if (_entries.IsEmpty) return 0;

        var cutoff = DateTime.UtcNow - ttl;
        var closed = 0;

        foreach (var (associationId, entry) in _entries)
        {
            if (entry.Context.ConnectedAt > cutoff) continue;

            _ = Close(associationId, new AssociationSummary
            {
                Outcome = AssociationOutcome.Orphaned,
                Reason  = $"No close callback within {ttl.TotalMinutes:N0} min",
            });
            closed++;
        }

        return closed;
    }

    // ── ILogEventSink ────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        if (_disposed || _entries.IsEmpty) return;

        if (logEvent.Properties.GetValueOrDefault(DiagnosticsConstants.AssociationId)
            is not ScalarValue { Value: string associationId })
            return;

        if (!_entries.TryGetValue(associationId, out var entry))
            return;

        if (!Options.IncludeFoDicomInternals && IsFoDicomInternal(logEvent))
            return;

        try
        {
            entry.Logger.Write(logEvent);
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Failed to write association event for {0}: {1}", associationId, ex);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool IsFoDicomInternal(LogEvent logEvent) =>
        logEvent.Properties.GetValueOrDefault(SourceContextProperty)
            is ScalarValue { Value: string source } &&
        source.StartsWith(FoDicomSourcePrefix, StringComparison.Ordinal);

    /// <summary>
    /// Reserves one slot in the current day's budget. Counts existing files once per day so a
    /// restart does not reset the cap.
    /// </summary>
    private bool TryReserveDailySlot(string directory, int maxFilesPerDay, string associationId)
    {
        lock (_dayGate)
        {
            if (!string.Equals(_dayKey, directory, StringComparison.OrdinalIgnoreCase))
            {
                _dayKey = directory;
                _filesToday = Directory.Exists(directory)
                    ? Directory.EnumerateFiles(directory, "*.log").Count()
                    : 0;
            }

            if (_filesToday >= maxFilesPerDay)
            {
                SelfLog.WriteLine(
                    "Association log not opened for {0}: MaxFilesPerDay={1} reached",
                    associationId, maxFilesPerDay);
                return false;
            }

            _filesToday++;
            return true;
        }
    }

    private static string BuildFileName(AssociationLogContext context)
    {
        var timestamp = context.ConnectedAt.ToLocalTime()
            .ToString("yyyyMMdd-HHmmss.fff", CultureInfo.InvariantCulture);

        return $"{timestamp}_{Sanitize(context.CallingAe, 16)}_{Sanitize(context.RemoteHost, 39)}_{context.AssociationId}.log";
    }

    /// <summary>Replaces characters that are invalid in a file name and truncates.</summary>
    private static string Sanitize(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return "unknown";

        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(Math.Min(value.Length, maxLength));

        foreach (var c in value.Trim())
        {
            if (builder.Length == maxLength) break;
            builder.Append(invalid.Contains(c) || char.IsWhiteSpace(c) ? '_' : c);
        }

        return builder.Length == 0 ? "unknown" : builder.ToString();
    }

    private static LogEventLevel ParseLevel(string level) =>
        Enum.TryParse<LogEventLevel>(level, true, out var parsed) ? parsed : LogEventLevel.Debug;

    private const string Rule =
        "════════════════════════════════════════════════════════════════════════";

    private const string ThinRule =
        "────────────────────────────────────────────────────────────────────────";

    private static string BuildHeader(AssociationLogContext context) =>
        $"""
         {Rule}
          ASSOCIATION {context.AssociationId}
          Started    : {context.ConnectedAt:yyyy-MM-dd HH:mm:ss.fff} UTC
          Calling AE : {context.CallingAe}
          Called  AE : {context.CalledAe}
          Remote     : {context.RemoteHost}:{context.RemotePort}
         {Rule}

         """;

    private static string BuildFooter(Entry entry, AssociationSummary summary)
    {
        var duration = DateTime.UtcNow - entry.Context.ConnectedAt;
        var megabytes = summary.BytesReceived / 1024d / 1024d;
        var reason = string.IsNullOrWhiteSpace(summary.Reason) ? string.Empty : $"  reason={summary.Reason}";

        return $"""
                {ThinRule}
                 SUMMARY  status={summary.Outcome}  duration={duration.TotalSeconds:N1}s{reason}
                 C-STORE  ok={summary.CStoreOk}  failed={summary.CStoreFailed}  bytes={megabytes:N1} MB
                 C-FIND   mwl={summary.CFindMwl}  qr={summary.CFindQr}  results={summary.CFindResults}
                 C-ECHO   {summary.CEcho}  errors={summary.Errors}
                {Rule}

                """;
    }

    private static void WriteOpenedEvent(Entry entry, AssociationLogContext context) =>
        entry.Logger.Information(
            "AssociationOpened — Id={AssociationId} CallingAE={CallingAeTitle} CalledAE={CalledAeTitle} Remote={RemoteHost}:{RemotePort}",
            context.AssociationId, context.CallingAe, context.CalledAe, context.RemoteHost, context.RemotePort);

    private static void WriteClosedEvent(Entry entry, AssociationSummary summary) =>
        entry.Logger.Information(
            "AssociationClosed — Id={AssociationId} Status={AssociationOutcome} DurationMs={DurationMs} " +
            "CStoreOk={CStoreOk} CStoreFailed={CStoreFailed} Bytes={BytesReceived} " +
            "CFindMwl={CFindMwl} CFindQr={CFindQr} CFindResults={CFindResults} CEcho={CEcho} Errors={Errors} Reason={Reason}",
            entry.Context.AssociationId, summary.Outcome,
            (long)(DateTime.UtcNow - entry.Context.ConnectedAt).TotalMilliseconds,
            summary.CStoreOk, summary.CStoreFailed, summary.BytesReceived,
            summary.CFindMwl, summary.CFindQr, summary.CFindResults, summary.CEcho, summary.Errors,
            summary.Reason);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var (associationId, _) in _entries)
        {
            _ = Close(associationId, new AssociationSummary
            {
                Outcome = AssociationOutcome.Orphaned,
                Reason  = "Node shutting down",
            });
        }
    }

    private sealed record Entry(Serilog.Core.Logger Logger, string FilePath, AssociationLogContext Context);
}
