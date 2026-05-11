using Dicom.Edge.Node.Router;
using Dicom.Edge.Node.Sender;
using Dicom.Edge.Node.Persistence.Diagnostics;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// Loads routing rules from the SQLite database and feeds them into
/// <see cref="RuleBasedStudyRouter"/> on startup and when configuration changes.
/// </summary>
public sealed class RoutingRuleLoaderService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    RuleBasedStudyRouter router,
    INodeSettingsService settingsService,
    ILogger<RoutingRuleLoaderService> logger) : BackgroundService
{
    /// <summary>
    /// How often to re-read routing rules (picks up Hub-pushed changes).
    /// </summary>
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RoutingRuleLoaderService started");

        // Initial load
        await LoadRulesAsync(stoppingToken);

        // Periodic reload
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ReloadInterval, stoppingToken);
                await LoadRulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reload routing rules — retrying in {Interval}", ReloadInterval);
            }
        }
    }

    private async Task LoadRulesAsync(CancellationToken ct)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var dbRules = await ctx.RoutingRules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .AsNoTracking()
            .ToListAsync(ct);

        var routingRules = dbRules.Select(r => new Router.RoutingRule
        {
            Id = r.Id,
            Name = r.Name,
            Priority = r.Priority,
            IsEnabled = r.IsEnabled,
            ModalityFilter = r.Modality,
            SourceAeTitleFilter = r.SourceAeTitle,
            InstitutionFilter = r.InstitutionName,
            UrgentOnly = false,
            Destination = new PacsDestination
            {
                Id = r.Id,
                AeTitle = r.DestinationAeTitle,
                Host = "localhost",
                Port = 104
            }
        }).ToList();

        // Try to resolve the default destination from node settings
        PacsDestination? defaultDestination = null;
        var defaultAe = await settingsService.GetAsync<string>("pacs.default_destination_ae", string.Empty, ct);
        if (!string.IsNullOrEmpty(defaultAe))
        {
            var defaultHost = await settingsService.GetAsync<string>("pacs.default_destination_host", "localhost", ct);
            var defaultPort = await settingsService.GetAsync<int>("pacs.default_destination_port", 104, ct);

            defaultDestination = new PacsDestination
            {
                Id = "default",
                AeTitle = defaultAe,
                Host = defaultHost,
                Port = defaultPort
            };
        }

        router.LoadRules(routingRules, defaultDestination);

        logger.LogInformation(
            "Loaded {RuleCount} routing rules from database (default destination: {DefaultAe})",
            routingRules.Count, defaultDestination?.AeTitle ?? "none");
    }
}
