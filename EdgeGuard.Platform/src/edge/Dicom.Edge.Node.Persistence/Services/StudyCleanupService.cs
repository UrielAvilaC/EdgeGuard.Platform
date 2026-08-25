using System.Diagnostics;
using Dicom.Edge.Abstractions.Metrics;
using Dicom.Edge.Abstractions.Storage;
using Dicom.Edge.Node.Persistence.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Background service that enforces storage retention policies.
/// Runs on a configurable interval (<c>cleanup.run_interval_minutes</c>).
/// All thresholds are read from <see cref="INodeSettingsService"/> on every cycle
/// so changes take effect without restarting the node.
/// <para>
/// <strong>Cleanup phases (in order):</strong>
/// <list type="number">
///   <item>Soft-delete sent studies older than <c>cleanup.retain_sent_days</c>.</item>
///   <item>Soft-delete failed studies older than <c>cleanup.retain_failed_days</c>.</item>
///   <item>Soft-delete any study older than <c>cleanup.retain_days</c> regardless of status.</item>
///   <item>Purge physical files + hard-delete instances/series for soft-deleted studies.</item>
///   <item>Purge audit logs older than <c>security.audit_retention_days</c>.</item>
///   <item>Emergency storage pressure cleanup when total storage exceeds <c>cleanup.max_storage_gb</c>.</item>
/// </list>
/// </para>
/// </summary>
public sealed class StudyCleanupService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    INodeSettingsService settings,
    IStorageUsageProbe usageProbe,
    IMetricsCollector metrics,
    ILogger<StudyCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StudyCleanupService started");

        // First cycle slightly delayed so startup I/O settles
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cleanup = await settings.GetCleanupConfigAsync(stoppingToken);

                if (cleanup.Enabled)
                    await RunCleanupCycleAsync(cleanup, stoppingToken);

                await Task.Delay(
                    TimeSpan.FromMinutes(cleanup.RunIntervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                metrics.RecordError("StudyCleanup", ex.Message, severity: 2);
                logger.LogError(ex, "Unhandled error in cleanup cycle — retrying in 5 minutes");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    // ── Cycle ─────────────────────────────────────────────────────────────────

    private async Task RunCleanupCycleAsync(CleanupConfig cleanup, CancellationToken ct)
    {
        using var cycleActivity = PersistenceActivitySource.StartCleanupCycle();
        logger.LogDebug("Cleanup cycle starting");
        var storage = await settings.GetStorageConfigAsync(ct);

        await SoftDeleteSentStudiesAsync(cleanup, ct);
        await SoftDeleteFailedStudiesAsync(cleanup, ct);
        await SoftDeleteExpiredStudiesAsync(cleanup, ct);
        await PurgePhysicalFilesAsync(storage, ct);
        await PurgeAuditLogsAsync(ct);
        await EmergencyStorageCleanupAsync(cleanup, ct);

        cycleActivity?.SetStatus(ActivityStatusCode.Ok);
        logger.LogDebug("Cleanup cycle complete");
    }

    // ── Phase 1: Soft-delete sent studies ────────────────────────────────────

    private async Task SoftDeleteSentStudiesAsync(CleanupConfig cfg, CancellationToken ct)
    {
        using var activity = PersistenceActivitySource.StartCleanupPhase("SoftDeleteSent");
        var cutoff = DateTime.UtcNow.AddDays(-cfg.RetainSentDays);
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var studies = await ctx.Studies
            .Where(s => s.Status == StudyStatus.SentToPacs
                     && s.SentToHubAt < cutoff)
            .ToListAsync(ct);

        if (studies.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var s in studies)
        {
            s.IsDeleted = true;
            s.DeletedAt = now;
            ctx.Entry(s).Property<DateTime>("updated_at").CurrentValue = now;
        }

        await ctx.SaveChangesAsync(ct);
        activity?.SetTag("persistence.cleanup.deleted", studies.Count);
        logger.LogInformation("Soft-deleted {Count} sent studies (>{Days} days)", studies.Count, cfg.RetainSentDays);
    }

    // ── Phase 2: Soft-delete failed studies ──────────────────────────────────

    private async Task SoftDeleteFailedStudiesAsync(CleanupConfig cfg, CancellationToken ct)
    {
        using var activity = PersistenceActivitySource.StartCleanupPhase("SoftDeleteFailed");
        var cutoff = DateTime.UtcNow.AddDays(-cfg.RetainFailedDays);
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var studies = await ctx.Studies
            .Where(s => s.Status == StudyStatus.Failed
                     && s.ReceivedAt < cutoff)
            .ToListAsync(ct);

        if (studies.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var s in studies)
        {
            s.IsDeleted = true;
            s.DeletedAt = now;
            ctx.Entry(s).Property<DateTime>("updated_at").CurrentValue = now;
        }

        await ctx.SaveChangesAsync(ct);
        activity?.SetTag("persistence.cleanup.deleted", studies.Count);
        logger.LogInformation("Soft-deleted {Count} failed studies (>{Days} days)", studies.Count, cfg.RetainFailedDays);
    }

    // ── Phase 3: Soft-delete any study past global retention ─────────────────

    private async Task SoftDeleteExpiredStudiesAsync(CleanupConfig cfg, CancellationToken ct)
    {
        using var activity = PersistenceActivitySource.StartCleanupPhase("SoftDeleteExpired");
        var cutoff = DateTime.UtcNow.AddDays(-cfg.RetainDays);
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var studies = await ctx.Studies
            .Where(s => s.ReceivedAt < cutoff)
            .ToListAsync(ct);

        if (studies.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var s in studies)
        {
            s.IsDeleted = true;
            s.DeletedAt = now;
            ctx.Entry(s).Property<DateTime>("updated_at").CurrentValue = now;
        }

        await ctx.SaveChangesAsync(ct);
        activity?.SetTag("persistence.cleanup.deleted", studies.Count);
        logger.LogInformation("Soft-deleted {Count} expired studies (>{Days} days)", studies.Count, cfg.RetainDays);
    }

    // ── Phase 4: Purge physical files for soft-deleted studies ───────────────

    private async Task PurgePhysicalFilesAsync(StorageConfig storage, CancellationToken ct)
    {
        using var activity = PersistenceActivitySource.StartCleanupPhase("PurgeFiles");
        await using var ctx = await factory.CreateDbContextAsync(ct);

        // Load soft-deleted studies that still have instance rows
        // Must bypass global query filter to see is_deleted = 1 rows
        var studyUids = await ctx.Studies
            .IgnoreQueryFilters()
            .Where(s => s.IsDeleted && s.DeletedAt.HasValue)
            .Select(s => s.StudyInstanceUid)
            .ToListAsync(ct);

        if (studyUids.Count == 0) return;

        var totalFiles = 0;
        long totalBytes = 0;

        foreach (var uid in studyUids)
        {
            try
            {
                using var purgeActivity = PersistenceActivitySource.StartPurgeFiles(uid);

                // Single query: join series → instances for this study
                var instances = await ctx.Instances
                    .Where(i => ctx.Series
                        .Where(sr => sr.StudyInstanceUid == uid)
                        .Select(sr => sr.SeriesInstanceUid)
                        .Contains(i.SeriesInstanceUid))
                    .ToListAsync(ct);

                // Delete physical files
                foreach (var instance in instances)
                {
                    if (!File.Exists(instance.FilePath)) continue;
                    var info = new FileInfo(instance.FilePath);
                    totalBytes += info.Length;
                    File.Delete(instance.FilePath);
                    totalFiles++;
                }

                // Hard-delete instances, series, and queue items in a single SaveChanges
                ctx.Instances.RemoveRange(instances);

                var series = await ctx.Series
                    .Where(sr => sr.StudyInstanceUid == uid)
                    .ToListAsync(ct);
                ctx.Series.RemoveRange(series);

                var queueItems = await ctx.QueueItems
                    .Where(q => q.StudyInstanceUid == uid)
                    .ToListAsync(ct);
                ctx.QueueItems.RemoveRange(queueItems);

                await ctx.SaveChangesAsync(ct);

                purgeActivity?.SetTag("persistence.purge.instances", instances.Count);

                // Remove empty study directories
                TryRemoveStudyDirectory(storage.RootPath, uid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error purging files for study {StudyUid}", uid);
            }
        }

        if (totalFiles > 0)
        {
            activity?.SetTag("persistence.purge.files", totalFiles);
            activity?.SetTag("persistence.purge.bytes", totalBytes);
            logger.LogInformation(
                "Purged {Files} files ({MB:F1} MB) from {Studies} studies",
                totalFiles, totalBytes / (1024.0 * 1024.0), studyUids.Count);
        }
    }

    // ── Phase 5: Purge old audit logs ─────────────────────────────────────────

    private async Task PurgeAuditLogsAsync(CancellationToken ct)
    {
        var security = await settings.GetSecurityConfigAsync(ct);
        var cutoff = DateTime.UtcNow.AddDays(-security.AuditRetentionDays);

        await using var ctx = await factory.CreateDbContextAsync(ct);
        var deleted = await ctx.AuditLogs
            .Where(a => a.Timestamp < cutoff)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            logger.LogInformation(
                "Purged {Count} audit log entries older than {Days} days",
                deleted, security.AuditRetentionDays);
    }

    // ── Reconciliación de deriva ──────────────────────────────────────────────

    /// <summary>
    /// Contrasta el contador de la base contra lo que hay realmente en disco.
    ///
    /// <para>El contador por estudio se mantiene al vuelo conforme llegan las
    /// instancias, y por eso es barato de consultar. Pero puede desviarse:
    /// estudios abortados a medio recibir, purgas que fallaron después de borrar
    /// filas, archivos que alguien movió a mano. Este recorrido es lo único que
    /// lo detecta, y por eso corre en la cadencia del cleanup y no en la del
    /// reporte.</para>
    ///
    /// <para>Detecta y avisa, pero no corrige. Corregir automáticamente un número
    /// que alimenta una decisión de purga merece más cuidado que una primera
    /// pasada: el recorrido compite con las instancias que están entrando, así
    /// que una diferencia puede ser deriva real o simplemente un estudio a medio
    /// escribir.</para>
    /// </summary>
    private async Task ReconcileStorageDriftAsync(StorageUsage usage, CancellationToken ct)
    {
        var storage = await settings.GetStorageConfigAsync(ct);
        if (!Directory.Exists(storage.RootPath)) return;

        long onDiskBytes;
        try
        {
            onDiskBytes = new DirectoryInfo(storage.RootPath)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch (IOException ex)
        {
            logger.LogDebug(ex, "Storage drift check skipped — could not walk {Root}", storage.RootPath);
            return;
        }

        var onDiskMb = onDiskBytes / (1024L * 1024L);
        var driftMb = Math.Abs(onDiskMb - usage.DicomMb);

        // Un umbral relativo evita ruido en nodos grandes y sigue siendo
        // sensible en los chicos.
        var toleranceMb = Math.Max(64, usage.DicomMb / 20);
        if (driftMb <= toleranceMb) return;

        logger.LogWarning(
            "Storage drift: disco reporta {OnDiskMb} MB bajo {Root} pero el contador de " +
            "estudios suma {CounterMb} MB (diferencia {DriftMb} MB). Puede haber archivos " +
            "huérfanos de estudios abortados o purgas incompletas.",
            onDiskMb, storage.RootPath, usage.DicomMb, driftMb);
    }

    // ── Phase 6: Emergency storage pressure cleanup ───────────────────────────

    private async Task EmergencyStorageCleanupAsync(CleanupConfig cfg, CancellationToken ct)
    {
        var usage = await usageProbe.MeasureAsync(ct);

        await ReconcileStorageDriftAsync(usage, ct);

        // 0 = sin límite. Un nodo sin cuota nunca entra en purga de emergencia.
        if (usage.LimitMb <= 0 || usage.TotalUsedMb <= usage.LimitMb) return;

        logger.LogWarning(
            "Storage pressure: {UsedMb} MB used ({DicomMb} DICOM + {DbMb} DB), limit is " +
            "{LimitMb} MB — triggering emergency cleanup",
            usage.TotalUsedMb, usage.DicomMb, usage.DatabaseMb, usage.LimitMb);

        // Soft-delete oldest SentToPacs studies until under threshold
        await using var ctx = await factory.CreateDbContextAsync(ct);
        var candidates = await ctx.Studies
            .Where(s => s.Status == StudyStatus.SentToPacs)
            .OrderBy(s => s.ReceivedAt)
            .Take(50)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var s in candidates)
        {
            s.IsDeleted = true;
            s.DeletedAt = now;
            ctx.Entry(s).Property<DateTime>("updated_at").CurrentValue = now;
        }

        await ctx.SaveChangesAsync(ct);
        logger.LogWarning(
            "Emergency cleanup: soft-deleted {Count} studies to relieve storage pressure",
            candidates.Count);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void TryRemoveStudyDirectory(string root, string studyUid)
    {
        try
        {
            var dir = Path.Combine(root, studyUid);
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                Directory.Delete(dir, recursive: false);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not remove study directory for {StudyUid}", studyUid);
        }
    }
}
