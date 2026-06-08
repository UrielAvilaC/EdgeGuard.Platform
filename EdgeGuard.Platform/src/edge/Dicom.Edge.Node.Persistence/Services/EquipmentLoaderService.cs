namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Loads the equipment catalog (with modality codes) from the SQLite database into the
/// in-memory <see cref="IEquipmentCatalog"/> on startup and periodically thereafter.
/// The sync endpoint calls <see cref="LoadNowAsync"/> after a Hub push so changes take
/// effect instantly without waiting for the periodic reload.
/// </summary>
public sealed class EquipmentLoaderService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    IEquipmentCatalog catalog,
    ILogger<EquipmentLoaderService> logger) : BackgroundService
{
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EquipmentLoaderService started");

        await LoadAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ReloadInterval, stoppingToken);
                await LoadAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reload equipment catalog — retrying in {Interval}", ReloadInterval);
            }
        }
    }

    /// <summary>Forces an immediate reload from the database (called by the sync endpoint).</summary>
    public Task LoadNowAsync(CancellationToken ct = default) => LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var equipment = await ctx.Equipment
            .Include(e => e.Modalities)
            .AsNoTracking()
            .ToListAsync(ct);

        var entries = equipment.Select(e => new EquipmentCatalogEntry(
            e.Id,
            e.AeTitle,
            e.Modalities.Select(m => m.ModalityCode).ToList(),
            e.StationAeTitle,
            e.IpAddress,
            e.IsEnabled)).ToList();

        catalog.Replace(entries);

        logger.LogInformation(
            "Loaded {Count} equipment entries into the in-memory catalog", entries.Count);
    }
}
