using Dicom.Edge.Node.Persistence.Configuration;
using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Seed;

/// <summary>
/// Copies the <c>HubConnection</c> section of appsettings into the <c>hub.*</c> rows of
/// <c>node_settings</c> — but only for rows that are still empty.
/// <para>
/// Runs right after migrations + seeding (see <c>PersistenceInitializerService</c>), before
/// anything binds <c>IOptions&lt;HubConnectionOptions&gt;</c>, so the node starts with the
/// values it was deployed with instead of hard-coded seed defaults.
/// </para>
/// <para>
/// Precedence rationale: <see cref="NodeDatabaseConfigurationProvider"/> is registered after
/// the JSON providers, so a non-empty DB row always wins over appsettings. Seeding hub rows
/// empty + hydrating them here keeps appsettings authoritative on first run, and the DB
/// (Hub UI edits, Hub push) authoritative afterwards.
/// </para>
/// </summary>
internal static class NodeSettingsHubHydrator
{
    /// <summary>
    /// Fills every empty <c>hub.*</c> row from appsettings. Idempotent: rows that already
    /// hold a value (previous run, Hub UI edit, Hub config push) are never touched.
    /// </summary>
    /// <returns>Number of rows hydrated.</returns>
    public static async Task<int> HydrateFromConfigurationAsync(
        EdgeNodeDbContext ctx,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var values = BuildFromConfiguration(configuration);
        if (values.Count == 0) return 0;

        var keys = values.Keys.ToList();
        var rows = await ctx.NodeSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(ct);

        var hydrated = new List<string>();

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.Value)) continue;
            if (!values.TryGetValue(row.Key, out var value) || string.IsNullOrWhiteSpace(value)) continue;

            row.Value = value;
            row.UpdatedAt = DateTime.UtcNow;
            hydrated.Add($"{row.Key}={value}");
        }

        if (hydrated.Count == 0)
        {
            logger.LogDebug("Hub settings hydration: nothing to do — all hub.* rows already have a value");
            return 0;
        }

        await ctx.SaveChangesAsync(ct);

        logger.LogInformation(
            "Hub settings hydrated from appsettings — {Count} empty row(s) filled: [{Settings}]",
            hydrated.Count, string.Join(", ", hydrated));

        return hydrated.Count;
    }

    // ── appsettings → DB key map ─────────────────────────────────────────────

    /// <summary>
    /// Reads the <c>HubConnection</c> section through <see cref="IConfiguration"/> (so env vars
    /// keep their precedence over appsettings) and projects it onto <c>hub.*</c> setting keys.
    /// Keys absent from configuration are omitted so the seed row stays empty.
    /// </summary>
    private static Dictionary<string, string> BuildFromConfiguration(IConfiguration configuration)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(map, NodeSettingKeys.Hub.Enabled,               configuration[ConfigPaths.HubEnabled]?.ToLowerInvariant());
        Add(map, NodeSettingKeys.Hub.TimeoutSeconds,        configuration[ConfigPaths.HubTimeoutSeconds]);
        Add(map, NodeSettingKeys.Hub.HeartbeatIntervalSec,  configuration[ConfigPaths.HubHeartbeatIntervalSeconds]);
        Add(map, NodeSettingKeys.Hub.RegisterOnStartup,     configuration[ConfigPaths.HubRegisterOnStartup]?.ToLowerInvariant());
        Add(map, NodeSettingKeys.Hub.MaxReconnectAttempts,  configuration[ConfigPaths.HubMaxReconnectAttempts]);
        Add(map, NodeSettingKeys.Hub.ReconnectDelaySeconds, configuration[ConfigPaths.HubReconnectDelaySeconds]);

        // HubBaseUrl is a single composed URL in appsettings; the DB stores protocol,
        // hostname and port separately so the Hub UI can edit each part independently.
        // The port is always persisted (never left empty) because the configuration
        // provider recomposes the URL as "{protocol}://{hostname}:{port}".
        if (Uri.TryCreate(configuration[ConfigPaths.HubBaseUrl], UriKind.Absolute, out var hubUri))
        {
            Add(map, NodeSettingKeys.Hub.Protocol, hubUri.Scheme);
            Add(map, NodeSettingKeys.Hub.Hostname, hubUri.Host);
            Add(map, NodeSettingKeys.Hub.Port,     hubUri.Port.ToString(CultureInfo.InvariantCulture));
        }

        // Stored as minutes in the DB, exposed as seconds in HubConnectionOptions.
        if (int.TryParse(configuration[ConfigPaths.HubConfigPullIntervalSeconds], out var pullSeconds) &&
            pullSeconds > 0)
        {
            var minutes = Math.Max(1, pullSeconds / 60);
            Add(map, NodeSettingKeys.Hub.PullConfigIntervalMin, minutes.ToString(CultureInfo.InvariantCulture));
        }

        // ApiKey and NodeId are intentionally excluded — they are written only after a
        // successful Hub registration, never seeded from appsettings (stale credentials).

        return map;
    }

    private static void Add(Dictionary<string, string> map, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            map[key] = value;
    }
}
