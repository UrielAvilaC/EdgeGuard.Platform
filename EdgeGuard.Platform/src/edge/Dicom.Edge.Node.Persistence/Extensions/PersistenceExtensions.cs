using Dicom.Edge.Abstractions.Queue;
using Dicom.Edge.Node.Persistence.Interceptors;
using Dicom.Edge.Node.Persistence.Diagnostics;
using Dicom.Edge.Node.Persistence.Queue;
using Dicom.Edge.Node.Persistence.Repositories;
using Dicom.Edge.Node.Persistence.Services;
using Dicom.Edge.Node.Persistence.Seed;
using Dicom.Edge.Node.Persistence.UnitOfWork;
using Microsoft.Extensions.Configuration;
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
    /// Migrations are <strong>not</strong> applied automatically; run <c>dotnet ef database update</c> manually.
    /// </summary>
    public static IServiceCollection AddEdgePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbPath = configuration["Persistence:DatabasePath"] ?? "edge-node.db";

        ConfigureDbContext(services, dbPath);
        RegisterInfrastructure(services);
        RegisterBackgroundServices(services);
        RegisterTracing(services);

        return services;
    }

    // ── DbContext ─────────────────────────────────────────────────────────────

    private static void ConfigureDbContext(IServiceCollection services, string dbPath)
    {
        var connectionString = $"Data Source={dbPath}";

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
        services.AddDbContext<EdgeNodeDbContext>(ConfigureOptions);
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<INodeSettingsService, NodeSettingsService>();
        services.AddSingleton<IEdgeQueue<EdgeQueueItem>, SqliteEdgeQueue>();
    }

    // ── Background services ───────────────────────────────────────────────────

    private static void RegisterBackgroundServices(IServiceCollection services)
    {
        services.AddHostedService<PersistenceInitializerService>();
        services.AddHostedService<StudyCompletionWatcherService>();
        services.AddHostedService<StudyCleanupService>();
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
///   <item>Ensures the SQLite database and schema exist (EnsureCreated for first deployment,
///         then checks pending migrations for subsequent upgrades).</item>
///   <item>Runs <see cref="NodeSettingsSeed.SeedMissingAsync"/> to insert any new default settings.</item>
///   <item>Warms the <see cref="INodeSettingsService"/> in-memory cache via <see cref="INodeSettingsService.ReloadAsync"/>.</item>
/// </list>
/// Stops itself after completing — does not remain active during the application lifetime.
/// </summary>
internal sealed class PersistenceInitializerService(
    IDbContextFactory<EdgeNodeDbContext> factory,
    INodeSettingsService settingsService,
    ILogger<PersistenceInitializerService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = PersistenceActivitySource.StartSeedCheck();
        logger.LogInformation("Persistence initializer starting");

        await using var ctx = await factory.CreateDbContextAsync(cancellationToken);

        // ── Phase 1: Ensure database & schema exist ──────────────────────────
        // Edge Nodes deploy to remote sites without DBA access.
        // EnsureCreated() is idempotent and won't modify an existing DB.
        // For schema evolution we check pending migrations as a warning.
        var created = await ctx.Database.EnsureCreatedAsync(cancellationToken);
        if (created)
        {
            logger.LogInformation("SQLite database created and schema initialized");
        }
        else
        {
            // DB already existed — check migration drift
            try
            {
                var pending = (await ctx.Database
                    .GetPendingMigrationsAsync(cancellationToken)).ToList();

                if (pending.Count > 0)
                {
                    logger.LogWarning(
                        "There are {Count} pending migration(s): [{Migrations}]. " +
                        "Run 'dotnet ef database update' before starting the node.",
                        pending.Count, string.Join(", ", pending));
                }
                else
                {
                    logger.LogDebug("Database schema is up to date");
                }
            }
            catch (Exception ex)
            {
                // No migrations table yet (pre-migration deployment) — safe to ignore
                logger.LogDebug(ex, "Migration check skipped (no migration history table)");
            }
        }

        // ── Phase 2: Seed missing settings ───────────────────────────────────
        await NodeSettingsSeed.SeedMissingAsync(ctx, cancellationToken);
        logger.LogDebug("Seed check complete");

        // ── Phase 3: Warm settings cache ─────────────────────────────────────
        await settingsService.ReloadAsync(cancellationToken);
        logger.LogInformation("NodeSettings cache warmed ({Count} entries loaded)",
            (await ctx.NodeSettings.CountAsync(cancellationToken)));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
