using Dicom.Edge.Node.Persistence.Repositories;
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
    INodePacsServerRepository pacsServerRepository,
    RuleBasedStudyRouter router,
    ILogger<RoutingRuleLoaderService> logger) : BackgroundService
{
    /// <summary>
    /// How often to re-read routing rules (picks up Hub-pushed changes).
    /// </summary>
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RoutingRuleLoaderService started");

        await LoadRulesAsync(stoppingToken);

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

    /// <summary>
    /// Forces an immediate reload of routing rules from the database.
    /// Called by the sync endpoint after a Hub push so rules take effect instantly
    /// without waiting for the periodic <see cref="ReloadInterval"/>.
    /// </summary>
    public Task LoadNowAsync(CancellationToken ct = default) => LoadRulesAsync(ct);

    private async Task LoadRulesAsync(CancellationToken ct)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        // Build a lookup of AeTitle → NodePacsServer for host/port resolution
        var pacsServers = await pacsServerRepository.GetAllEnabledAsync(ct);
        var pacsLookup  = pacsServers.ToDictionary(p => p.AeTitle, StringComparer.OrdinalIgnoreCase);

        var dbRules = await ctx.RoutingRules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.Priority)
            .AsNoTracking()
            .ToListAsync(ct);

        var routingRules = dbRules.Select(r =>
        {
            pacsLookup.TryGetValue(r.DestinationAeTitle, out var pacs);
            return new Router.RoutingRule
            {
                Id                   = r.Id,
                Name                 = r.Name,
                Priority             = r.Priority,
                IsEnabled            = r.IsEnabled,
                ModalityFilter       = r.Modality,
                SourceAeTitleFilter  = r.SourceAeTitle,
                InstitutionFilter    = r.InstitutionName,
                UrgentOnly           = false,
                Destination          = new PacsDestination
                {
                    Id      = r.Id,
                    AeTitle = r.DestinationAeTitle,
                    Host    = pacs?.Host ?? "localhost",
                    Port    = pacs?.Port ?? 104,
                },
            };
        }).ToList();

        // Default destinations: all enabled PACS servers ordered by priority.
        // When no routing rule matches a study, it is sent to every default destination in parallel.
        var defaultDestinations = pacsServers
            .OrderBy(p => p.Priority)
            .Select(p => new PacsDestination
            {
                Id      = p.Id,
                AeTitle = p.AeTitle,
                Host    = p.Host,
                Port    = p.Port,
            })
            .ToList();

        router.LoadRules(routingRules, defaultDestinations);

        logger.LogInformation(
            "Loaded {RuleCount} routing rules from database ({DefaultCount} default destination(s): [{DefaultAes}])",
            routingRules.Count,
            defaultDestinations.Count,
            string.Join(", ", defaultDestinations.Select(d => d.AeTitle)));
    }
}
