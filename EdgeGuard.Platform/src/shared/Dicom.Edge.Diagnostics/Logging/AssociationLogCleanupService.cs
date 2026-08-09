using Dicom.Edge.Diagnostics.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.Logging;

/// <summary>
/// Enforces retention on the per-association log directory and closes association files whose
/// close callback never arrived.
/// </summary>
/// <remarks>
/// Runs on a configurable interval and re-reads options on every cycle, so retention changes
/// pushed from the Hub take effect without restarting the node. Never throws out of the loop:
/// a cleanup failure must not stop the service.
/// <para>Cycle order: sweep stale handles → delete day folders past retention → enforce the
/// total size cap by deleting the oldest days first.</para>
/// </remarks>
public sealed class AssociationLogCleanupService(
    IAssociationLogWriter writer,
    IOptionsMonitor<DiagnosticsOptions> optionsMonitor,
    ILogger<AssociationLogCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup I/O settle before the first cycle.
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = optionsMonitor.CurrentValue.File.PerAssociation;

            try
            {
                if (options.Enabled)
                    RunRetentionCycle();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Association log cleanup cycle failed — retrying next interval");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(Math.Max(1, options.CleanupIntervalMinutes)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;     // expected on shutdown
            }
        }
    }

    /// <summary>
    /// Runs one retention pass immediately with the current options: closes stale association
    /// files, deletes day folders past retention and enforces the total size cap.
    /// Public so an operator action (or a verification harness) can trigger it out of band.
    /// </summary>
    /// <returns>Number of day folders deleted.</returns>
    public int RunRetentionCycle()
    {
        var options = optionsMonitor.CurrentValue.File.PerAssociation;

        var orphaned = writer.SweepStale(TimeSpan.FromMinutes(Math.Max(1, options.StaleTimeoutMinutes)));
        if (orphaned > 0)
            logger.LogWarning(
                "Closed {Count} association log(s) with no release/abort callback (stale > {Minutes} min)",
                orphaned, options.StaleTimeoutMinutes);

        var root = new DirectoryInfo(Path.GetFullPath(options.Path));
        if (!root.Exists) return 0;

        // Day folders are named yyyy-MM-dd; anything else is left alone.
        var days = root.GetDirectories()
            .Where(d => DateTime.TryParseExact(
                d.Name, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .ToList();

        var deletedDays = 0;
        long freedBytes = 0;

        // ── Phase 1: age ────────────────────────────────────────────────────
        var cutoff = DateTime.Today.AddDays(-Math.Max(0, options.RetainDays));
        foreach (var day in days.ToList())
        {
            var date = DateTime.ParseExact(day.Name, "yyyy-MM-dd", null);
            if (date >= cutoff) continue;

            freedBytes += DirectorySize(day);
            if (TryDelete(day)) { deletedDays++; days.Remove(day); }
        }

        // ── Phase 2: total size cap (oldest first) ──────────────────────────
        var capBytes = options.MaxTotalSizeMb * 1024L * 1024L;
        var totalBytes = days.Sum(DirectorySize);

        foreach (var day in days)
        {
            if (totalBytes <= capBytes) break;

            var size = DirectorySize(day);
            if (!TryDelete(day)) continue;

            totalBytes  -= size;
            freedBytes  += size;
            deletedDays++;
        }

        if (deletedDays > 0)
            logger.LogInformation(
                "Association log cleanup — {Days} day folder(s) removed, {Mb:N1} MB freed",
                deletedDays, freedBytes / 1024d / 1024d);

        return deletedDays;
    }

    private static long DirectorySize(DirectoryInfo directory)
    {
        try
        {
            return directory.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0;   // file vanished mid-enumeration or access denied
        }
    }

    private bool TryDelete(DirectoryInfo directory)
    {
        try
        {
            directory.Delete(recursive: true);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete association log folder {Folder}", directory.FullName);
            return false;
        }
    }
}
