using Dicom.Edge.Abstractions.Storage;
using Microsoft.Data.Sqlite;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Mide el consumo del nodo sin recorrer el disco.
///
/// <para>El peso DICOM sale de un <c>SUM</c> sobre el contador que el nodo ya
/// mantiene por estudio conforme llegan las instancias, así que es un agregado
/// indexado y no una tormenta de <c>stat</c>. El recorrido completo del árbol
/// sigue existiendo en <see cref="StudyCleanupService"/>, pero ahí cumple otro
/// papel: reconciliar la deriva entre el contador y lo que hay realmente en
/// disco — huérfanos de estudios abortados, purgas a medias, borrados manuales.</para>
///
/// <para>El resultado se cachea porque varios llamadores pueden pedirlo en el
/// mismo ciclo, y porque el valor no cambia lo bastante rápido como para
/// justificar recalcularlo en cada uno.</para>
/// </summary>
public sealed class StorageUsageProbe(
    IDbContextFactory<EdgeNodeDbContext> factory,
    INodeSettingsService settings,
    IStorageProvider storageProvider,
    ILogger<StorageUsageProbe> logger) : IStorageUsageProbe
{
    /// <summary>
    /// Ventana de cacheo. Corta pero no cero: absorbe llamadas repetidas dentro
    /// de un mismo ciclo sin volver stale el dato que se reporta.
    /// </summary>
    private static readonly TimeSpan CacheWindow = TimeSpan.FromSeconds(30);

    private const long BytesPerMb = 1024L * 1024L;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private StorageUsage? _cached;

    public async Task<StorageUsage> MeasureAsync(CancellationToken ct = default)
    {
        if (_cached is { } hit && DateTime.UtcNow - hit.MeasuredAt < CacheWindow)
            return hit;

        await _gate.WaitAsync(ct);
        try
        {
            // Otro llamador pudo medir mientras se esperaba el turno.
            if (_cached is { } fresh && DateTime.UtcNow - fresh.MeasuredAt < CacheWindow)
                return fresh;

            var usage = await MeasureCoreAsync(ct);
            _cached = usage;
            return usage;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<StorageUsage> MeasureCoreAsync(CancellationToken ct)
    {
        var storageConfig = await settings.GetStorageConfigAsync(ct);

        await using var ctx = await factory.CreateDbContextAsync(ct);

        // El filtro global de la entidad ya excluye los borrados lógicos.
        var dicomBytes = await ctx.Studies.SumAsync(s => s.TotalSizeBytes, ct);

        var databaseBytes = MeasureDatabaseBytes(ctx.Database.GetConnectionString());

        var (volumeFreeMb, volumeTotalMb) = await MeasureVolumeAsync(ct);

        var usage = new StorageUsage(
            DicomMb: dicomBytes / BytesPerMb,
            DatabaseMb: databaseBytes / BytesPerMb,
            VolumeFreeMb: volumeFreeMb,
            VolumeTotalMb: volumeTotalMb,
            LimitMb: storageConfig.LimitMb,
            MeasuredAt: DateTime.UtcNow);

        logger.LogDebug(
            "Storage measured: DICOM={DicomMb}MB DB={DbMb}MB limit={LimitMb}MB volume free={FreeMb}MB",
            usage.DicomMb, usage.DatabaseMb, usage.LimitMb, usage.VolumeFreeMb);

        return usage;
    }

    /// <summary>
    /// Suma el archivo de base y sus dos acompañantes. El -wal puede tener
    /// decenas de MB sin checkpoint, así que omitirlo subestimaría el consumo
    /// justo cuando el nodo está más ocupado.
    /// </summary>
    private long MeasureDatabaseBytes(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return 0;

        string dbPath;
        try
        {
            dbPath = new SqliteConnectionStringBuilder(connectionString).DataSource;
        }
        catch (ArgumentException)
        {
            return 0;
        }

        if (string.IsNullOrWhiteSpace(dbPath) ||
            dbPath.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            return 0;

        var total = 0L;
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                var info = new FileInfo(dbPath + suffix);
                if (info.Exists) total += info.Length;
            }
            catch (IOException ex)
            {
                // Un archivo bloqueado no debe tumbar la medición completa.
                logger.LogDebug(ex, "Could not size {Path}", dbPath + suffix);
            }
        }

        return total;
    }

    private async Task<(long? FreeMb, long? TotalMb)> MeasureVolumeAsync(CancellationToken ct)
    {
        var free = await storageProvider.GetAvailableSpaceAsync(ct);
        var total = await storageProvider.GetTotalSpaceAsync(ct);

        return (
            free.IsSuccess ? free.Value / BytesPerMb : null,
            total.IsSuccess ? total.Value / BytesPerMb : null);
    }
}
