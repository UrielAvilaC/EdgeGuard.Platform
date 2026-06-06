using Dicom.Edge.Abstractions.Events;
using Dicom.Edge.Abstractions.Queue;
using Dicom.Edge.Node.Persistence.Configuration;
using Dicom.Edge.Node.Persistence.Interceptors;
using Dicom.Edge.Node.Persistence.Diagnostics;
using Dicom.Edge.Node.Persistence.Queue;
using Dicom.Edge.Node.Persistence.Repositories;
using Dicom.Edge.Node.Persistence.Services;
using Dicom.Edge.Node.Persistence.Seed;
using Dicom.Edge.Node.Persistence.UnitOfWork;
using Dicom.Edge.Node.Queue;
using Dicom.Edge.Node.Router;
using Dicom.Edge.Node.Worklist;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;

namespace Dicom.Edge.Node.Persistence.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers all Edge Node persistence services:
    /// <list type="bullet">
    ///   <item><see cref="EdgeNodeDbContext"/> via <see cref="IDbContextFactory{TContext}"/> (for BackgroundServices)
    ///     and scoped <c>AddDbContext</c> (for HTTP request scopes).</item>
    ///   <item>SQLite pragmas interceptor (WAL, cache, mmap, foreign keys).</item>
    ///   <item>Timestamp interceptor (auto CreatedAt / UpdatedAt).</item>
    ///   <item>Generic <see cref="IRepository{TEntity}"/> and <see cref="IUnitOfWork"/>.</item>
    ///   <item><see cref="INodeSettingsService"/> singleton with in-memory cache.</item>
    ///   <item><see cref="StudyCompletionWatcherService"/> BackgroundService.</item>
    ///   <item><see cref="StudyCleanupService"/> BackgroundService.</item>
    ///   <item><see cref="PersistenceInitializerService"/> — runs seed check on startup.</item>
    /// </list>
    /// Pending migrations are applied automatically on startup by <see cref="PersistenceInitializerService"/>.
    /// </summary>
    public static IServiceCollection AddEdgePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable(NodeDbPathEnvVar)
                                   is { Length: > 0 } envPath
            ? $"Data Source={envPath}"
            : configuration.GetConnectionString(ConnectionStringName)
              ?? DefaultConnectionString;

        // Ensure the directory for the SQLite file exists
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        ConfigureDbContext(services, connectionString);
        RegisterInfrastructure(services);
        RegisterBackgroundServices(services);
        RegisterTracing(services);

        // Database health check (readiness probe)
        services.AddHealthChecks()
            .AddDbContextCheck<EdgeNodeDbContext>(
                "database",
                HealthStatus.Unhealthy,
                ["ready", "database"]);

        return services;
    }

    /// <summary>Name of the connection string in <c>ConnectionStrings</c> section.</summary>
    public const string ConnectionStringName = "NodeDatabase";

    /// <summary>Fallback SQLite connection string when nothing is configured.</summary>
    public const string DefaultConnectionString = "Data Source=edge-node.db";

    /// <summary>
    /// Environment variable for the Node SQLite database file path.
    /// Takes precedence over <c>ConnectionStrings:NodeDatabase</c> in appsettings.
    /// </summary>
    public const string NodeDbPathEnvVar = "EDGEGUARD_NODE_DB_PATH";

    // ── DbContext ─────────────────────────────────────────────────────────────

    private static void ConfigureDbContext(IServiceCollection services, string connectionString)
    {
        void ConfigureOptions(DbContextOptionsBuilder opts) =>
            opts.UseSqlite(
                    connectionString,
                    sql =>
                    {
                        sql.MigrationsAssembly(typeof(EdgeNodeDbContext).Assembly.FullName);
                        sql.CommandTimeout(30);
                    })
                .AddInterceptors(
                    new TimestampInterceptor(),
                    new SqlitePragmaInterceptor())
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging(false);

        // Pooled factory — reuses DbContext instances to reduce GC pressure.
        // Pool size = 128 covers concurrent BackgroundServices + API request scopes.
        services.AddPooledDbContextFactory<EdgeNodeDbContext>(ConfigureOptions, poolSize: 128);

        // Scoped registration — used by repositories and UoW within request scopes.
        // Resolves from the pooled factory so both paths share the same pool.
        services.AddScoped(sp =>
            sp.GetRequiredService<IDbContextFactory<EdgeNodeDbContext>>().CreateDbContext());
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<INodeSettingsService, NodeSettingsService>();
        services.AddSingleton<IEdgeQueue<EdgeQueueItem>, SqliteEdgeQueue>();
        services.AddSingleton<INodeWorkQueue, SqliteNodeWorkQueue>();
        services.AddSingleton<IWorklistManager, SqliteWorklistManager>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddSingleton<INodePacsServerRepository, NodePacsServerRepository>();

        // Equipment catalog: in-memory cache read by the DICOM SCP (association + MWL).
        services.AddSingleton<IEquipmentCatalog, InMemoryEquipmentCatalog>();
    }

    // ── Background services ───────────────────────────────────────────────────

    private static void RegisterBackgroundServices(IServiceCollection services)
    {
        services.AddHostedService<PersistenceInitializerService>();
        // Register as singleton so IStudyCompletionTrigger can be injected elsewhere
        // (e.g., CStoreScp via DicomScpDependencies), then reuse the same instance as hosted service.
        services.AddSingleton<StudyCompletionWatcherService>();
        services.AddSingleton<IStudyCompletionTrigger>(sp =>
            sp.GetRequiredService<StudyCompletionWatcherService>());
        services.AddHostedService(sp =>
            sp.GetRequiredService<StudyCompletionWatcherService>());
        services.AddHostedService<StudyCleanupService>();
        services.AddHostedService<RoutingRuleLoaderService>();

        // Equipment loader: singleton so the sync endpoint can force an immediate reload
        // via LoadNowAsync, plus the same instance runs as the periodic hosted service.
        services.AddSingleton<EquipmentLoaderService>();
        services.AddHostedService(sp => sp.GetRequiredService<EquipmentLoaderService>());
    }

    // ── OpenTelemetry tracing ─────────────────────────────────────────────────

    private static void RegisterTracing(IServiceCollection services)
    {
        services.ConfigureOpenTelemetryTracerProvider(builder =>
            builder.AddSource(PersistenceActivitySource.SourceName));
    }
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Startup-only hosted service that:
/// <list type="number">
///   <item>Applies pending EF Core migrations (creates the SQLite database if it does not exist).</item>
///   <item>Runs <see cref="NodeSettingsSeed.SeedMissingAsync"/> to insert any new default settings.</item>
///   <item>Warms the <see cref="INodeSettingsService"/> in-memory cache via <see cref="INodeSettingsService.ReloadAsync"/>.</item>
/// </list>
/// Stops itself after completing — does not remain active during the application lifetime.
/// </summary>
internal sealed class PersistenceInitializerService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    INodeSettingsService settingsService,
    INodeConfigurationReloader configReloader,
    ILogger<PersistenceInitializerService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = PersistenceActivitySource.StartSeedCheck();
        logger.LogInformation("Persistence initializer starting");

        await using var ctx = await factory.CreateDbContextAsync(cancellationToken);

        // ── Phase 1: Apply pending migrations (creates DB if it does not exist) ─
        // MigrateAsync is idempotent: creates the database when absent,
        // applies only pending migrations, and records them in __EFMigrationsHistory.
        // Edge Nodes deploy to remote sites without DBA access, so auto-migration is required.
        var pending = (await ctx.Database
            .GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count > 0)
        {
            logger.LogInformation(
                "Applying {Count} pending migration(s): [{Migrations}]",
                pending.Count, string.Join(", ", pending));
        }

        await ctx.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database schema is up to date");

        // ── Phase 2: Seed missing settings ───────────────────────────────────
        await NodeSettingsSeed.SeedMissingAsync(ctx,logger, cancellationToken);
        await Seed.ModalityCatalogSeed.SeedAsync(ctx, cancellationToken);
        logger.LogDebug("Seed check complete");

        // ── Phase 3: Warm settings cache ─────────────────────────────────────
        await settingsService.ReloadAsync(cancellationToken);
        logger.LogInformation("NodeSettings cache warmed ({Count} entries loaded)",
            (await ctx.NodeSettings.CountAsync(cancellationToken)));

        // ── Phase 4: Sync node_pacs_servers → cecho.destinations ─────────────
        // Ensures PacsCEchoHostedService starts with the correct destinations
        // even when the Hub has not pushed a sync in this session.
        await SyncPacsCEchoDestinationsOnStartupAsync(ctx, settingsService, configReloader, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SyncPacsCEchoDestinationsOnStartupAsync(
        EdgeNodeDbContext ctx,
        INodeSettingsService settingsService,
        INodeConfigurationReloader configReloader,
        CancellationToken ct)
    {
        try
        {
            var servers = await ctx.NodePacsServers
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.Priority)
                .AsNoTracking()
                .ToListAsync(ct);

            if (servers.Count == 0)
            {
                logger.LogDebug("No enabled PACS servers in node_pacs_servers — skipping cecho.destinations sync");
                return;
            }

            var destinations = servers
                .Select(s => new { s.Id, s.AeTitle, s.Host, s.Port, UseTls = false })
                .ToArray();

            var json = System.Text.Json.JsonSerializer.Serialize(destinations);
            await settingsService.SetAsync(Constants.NodeSettingKeys.PacsCEcho.Destinations, json, ct);
            configReloader.Reload();

            logger.LogInformation(
                "Startup: cecho.destinations seeded from node_pacs_servers — {Count} destination(s): [{Destinations}]",
                servers.Count,
                string.Join(", ", servers.Select(s => $"{s.AeTitle}@{s.Host}:{s.Port}")));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Startup: failed to sync node_pacs_servers → cecho.destinations");
        }
    }
}
