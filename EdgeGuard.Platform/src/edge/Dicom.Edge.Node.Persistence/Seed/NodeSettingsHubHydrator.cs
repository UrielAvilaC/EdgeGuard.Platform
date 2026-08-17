using Dicom.Edge.Node.Persistence.Configuration;
using Microsoft.Extensions.Configuration;

namespace Dicom.Edge.Node.Persistence.Seed;

/// <summary>
/// Copies the <c>HubConnection</c> section of the deployed configuration (appsettings /
/// environment variables) into the <c>hub.*</c> rows of <c>node_settings</c>.
/// <para>
/// Runs right after migrations + seeding (see <c>PersistenceInitializerService</c>), before
/// anything binds <c>IOptions&lt;HubConnectionOptions&gt;</c>, so the node starts with the
/// values it was deployed with instead of hard-coded seed defaults.
/// </para>
/// <para>
/// Precedence rationale: appsettings is the source of truth for the Hub address and there are
/// no built-in defaults for it — a node whose appsettings lacks <c>HubConnection:HubBaseUrl</c>
/// while the Hub is enabled fails to start instead of silently dialling an invented endpoint.
/// Once the node is registered (it holds a <c>hub.api_key</c>), the DB becomes authoritative so
/// operator edits survive restarts; while it is unregistered — i.e. not configured yet — the
/// deployed values are rewritten on every start.
/// </para>
/// </summary>
internal static class NodeSettingsHubHydrator
{
    /// <summary>
    /// Writes the deployed <c>HubConnection</c> values into the <c>hub.*</c> rows.
    /// <list type="bullet">
    ///   <item><b>Unregistered node</b> (no <c>hub.api_key</c>): every row backed by the deployed
    ///   configuration is overwritten — the node is not configured yet, so appsettings wins.</item>
    ///   <item><b>Registered node</b>: only empty rows are filled, so Hub UI edits and pushes
    ///   made after registration are preserved.</item>
    /// </list>
    /// </summary>
    /// <returns>Number of rows written.</returns>
    /// <exception cref="InvalidOperationException">
    /// Hub integration is enabled but the deployed configuration has no usable
    /// <c>HubConnection:HubBaseUrl</c>.
    /// </exception>
    public static async Task<int> HydrateFromConfigurationAsync(
        EdgeNodeDbContext ctx,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        // A node with the Hub disabled has no Hub address to validate or hydrate.
        var enabled = Deployed(configuration, ConfigPaths.HubEnabled);
        if (!bool.TryParse(enabled, out var hubEnabled) || !hubEnabled)
        {
            logger.LogDebug("Hub settings hydration skipped — Hub integration is disabled");
            return 0;
        }

        var values = BuildFromConfiguration(configuration);

        var keys = values.Keys.ToList();
        var rows = await ctx.NodeSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(ct);

        // No API key yet ⇒ the node has never completed registration ⇒ it is not configured,
        // so the deployed configuration — not whatever a previous Hub push left behind — decides.
        var apiKey = await ctx.NodeSettings
            .Where(s => s.Key == NodeSettingKeys.Hub.ApiKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        var registered = !string.IsNullOrWhiteSpace(apiKey);

        var written = new List<string>();

        foreach (var row in rows)
        {
            if (registered && !string.IsNullOrWhiteSpace(row.Value)) continue;
            if (!values.TryGetValue(row.Key, out var value)) continue;
            if (string.Equals(row.Value, value, StringComparison.Ordinal)) continue;

            row.Value = value;
            row.UpdatedAt = DateTime.UtcNow;
            written.Add($"{row.Key}={value}");
        }

        if (written.Count == 0)
        {
            logger.LogDebug(
                "Hub settings hydration: nothing to do (registered={Registered})", registered);
            return 0;
        }

        await ctx.SaveChangesAsync(ct);

        logger.LogInformation(
            registered
                ? "Hub settings hydrated from appsettings — {Count} empty row(s) filled: [{Settings}]"
                : "Node is not registered — {Count} hub.* row(s) reset from appsettings: [{Settings}]",
            written.Count, string.Join(", ", written));

        return written.Count;
    }

    // ── appsettings → DB key map ─────────────────────────────────────────────

    /// <summary>
    /// Reads the <c>HubConnection</c> section from the deployed configuration and projects it
    /// onto <c>hub.*</c> setting keys. Keys absent from configuration are omitted so the seed
    /// row stays empty; the Hub address is mandatory and throws when missing.
    /// </summary>
    private static Dictionary<string, string> BuildFromConfiguration(IConfiguration configuration)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Add(map, NodeSettingKeys.Hub.Enabled,               Deployed(configuration, ConfigPaths.HubEnabled)?.ToLowerInvariant());
        Add(map, NodeSettingKeys.Hub.TimeoutSeconds,        Deployed(configuration, ConfigPaths.HubTimeoutSeconds));
        Add(map, NodeSettingKeys.Hub.HeartbeatIntervalSec,  Deployed(configuration, ConfigPaths.HubHeartbeatIntervalSeconds));
        Add(map, NodeSettingKeys.Hub.RegisterOnStartup,     Deployed(configuration, ConfigPaths.HubRegisterOnStartup)?.ToLowerInvariant());
        Add(map, NodeSettingKeys.Hub.MaxReconnectAttempts,  Deployed(configuration, ConfigPaths.HubMaxReconnectAttempts));
        Add(map, NodeSettingKeys.Hub.ReconnectDelaySeconds, Deployed(configuration, ConfigPaths.HubReconnectDelaySeconds));

        // HubBaseUrl is a single composed URL in appsettings; the DB stores protocol,
        // hostname and port separately so the Hub UI can edit each part independently.
        // The port is always persisted (never left empty) because the configuration
        // provider only recomposes the URL when all three parts are present.
        var hubBaseUrl = Deployed(configuration, ConfigPaths.HubBaseUrl);
        if (!Uri.TryCreate(hubBaseUrl, UriKind.Absolute, out var hubUri) ||
            (hubUri.Scheme != Uri.UriSchemeHttp && hubUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"Hub integration is enabled but '{ConfigPaths.HubBaseUrl}' is missing or not an " +
                $"absolute http/https URL (value: '{hubBaseUrl ?? "<null>"}'). The deployed " +
                "configuration is the source of truth for the Hub address — there is no built-in " +
                "default. Set it in appsettings.<Environment>.json or via the " +
                "HubConnection__HubBaseUrl environment variable (e.g. \"http://10.10.10.32:80\").");
        }

        Add(map, NodeSettingKeys.Hub.Protocol, hubUri.Scheme);
        Add(map, NodeSettingKeys.Hub.Hostname, hubUri.Host);
        Add(map, NodeSettingKeys.Hub.Port,     hubUri.Port.ToString(CultureInfo.InvariantCulture));

        // Stored as minutes in the DB, exposed as seconds in HubConnectionOptions.
        if (int.TryParse(Deployed(configuration, ConfigPaths.HubConfigPullIntervalSeconds), out var pullSeconds) &&
            pullSeconds > 0)
        {
            var minutes = Math.Max(1, pullSeconds / 60);
            Add(map, NodeSettingKeys.Hub.PullConfigIntervalMin, minutes.ToString(CultureInfo.InvariantCulture));
        }

        // ApiKey and NodeId are intentionally excluded — they are written only after a
        // successful Hub registration, never seeded from appsettings (stale credentials).

        return map;
    }

    /// <summary>
    /// Reads a configuration key from the deployed providers only (appsettings, environment
    /// variables, …), skipping <see cref="NodeDatabaseConfigurationProvider"/>. The DB provider
    /// has the highest precedence, so reading through <see cref="IConfiguration"/> would return
    /// the very rows this hydrator is about to rewrite instead of the deployed value.
    /// </summary>
    private static string? Deployed(IConfiguration configuration, string key)
    {
        if (configuration is not IConfigurationRoot root)
            return configuration[key];

        foreach (var provider in root.Providers.Reverse())
        {
            if (provider is NodeDatabaseConfigurationProvider) continue;
            if (provider.TryGet(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static void Add(Dictionary<string, string> map, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            map[key] = value;
    }
}
