using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;

namespace Dicom.Edge.Hub.Api.Extensions;

/// <summary>
/// Logs a structured diagnostic block at startup so operators can quickly verify
/// binding addresses, CORS origins, auth mode, SPA mode and environment — the most
/// common reasons requests from outside localhost are blocked.
/// </summary>
public static class StartupDiagnosticsExtensions
{
    /// <summary>
    /// Log category for this block.
    /// <para>
    /// Deliberately NOT <c>ILogger&lt;WebApplication&gt;</c>. That resolves to the category
    /// <c>Microsoft.AspNetCore.Builder.WebApplication</c>, and the platform Serilog setup
    /// applies <c>MinimumLevel.Override("Microsoft", Warning)</c> — which silently dropped
    /// every Information line below, so in production the block emitted nothing but two
    /// context-free warnings. Own category, own level, same pattern as SpaStaticFiles.
    /// </para>
    /// </summary>
    private const string LogCategory = "EdgeGuard.Startup";

    public static WebApplication LogStartupDiagnostics(this WebApplication app)
    {
        var logger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(LogCategory);

        // ── Environment ──────────────────────────────────────────────────────
        var env = app.Environment;
        logger.LogInformation(
            "=== EdgeGuard Hub starting === Environment={Environment} ApplicationName={App} ContentRoot={ContentRoot}",
            env.EnvironmentName, env.ApplicationName, env.ContentRootPath);

        // ── IIS / Hosting model ───────────────────────────────────────────────
        var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
        var isIis = processName.Equals("w3wp", StringComparison.OrdinalIgnoreCase)
                 || processName.Equals("iisexpress", StringComparison.OrdinalIgnoreCase);
        if (isIis)
        {
            logger.LogInformation(
                "Hosting model=IIS In-Process (w3wp) — Kestrel addresses are controlled by IIS binding; " +
                "IServerAddressesFeature will be empty. Verify IIS site bindings in IIS Manager.");
            logger.LogInformation(
                "IIS AppPool={AppPool} — if DataProtection warns about ephemeral keys, " +
                "enable 'Load User Profile=true' on the app pool or configure a key-persistence path.",
                Environment.GetEnvironmentVariable("APP_POOL_ID") ?? "(unknown)");
        }

        // ── Bound addresses ──────────────────────────────────────────────────
        // Available only after app.Build(); the server may not have started yet,
        // so we also log ASPNETCORE_URLS / Kestrel config as a fallback.
        var serverAddresses = app.Services
            .GetService<IServer>()
            ?.Features.Get<IServerAddressesFeature>()
            ?.Addresses;

        var aspnetUrls  = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        var kestrelUrls = app.Configuration["Kestrel:Endpoints:Http:Url"]
                       ?? app.Configuration["Kestrel:Endpoints:Https:Url"];

        if (serverAddresses is { Count: > 0 })
            logger.LogInformation("Bound addresses: {Addresses}", string.Join(", ", serverAddresses));
        else if (!string.IsNullOrWhiteSpace(aspnetUrls))
            logger.LogInformation("ASPNETCORE_URLS={Urls}", aspnetUrls);
        else if (!string.IsNullOrWhiteSpace(kestrelUrls))
            logger.LogInformation("Kestrel URL={Url}", kestrelUrls);
        else if (!isIis)
            logger.LogWarning("No bound address found — server may only listen on localhost:5000/5001 (default)");

        // ── DataProtection ────────────────────────────────────────────────────
        var dpKeyPath = app.Configuration["DataProtection:KeyPath"]
                     ?? app.Configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(dpKeyPath))
            logger.LogInformation("DataProtection keys persisted to {KeyPath}", dpKeyPath);
        else
            logger.LogWarning(
                "DataProtection:KeyPath not configured — keys are ephemeral (in-memory). " +
                "Under IIS this causes SignalR token and antiforgery invalidation on every app-pool recycle. " +
                "Set DataProtection:KeyPath in appsettings.Production.json to a writable directory, " +
                "e.g. C:\\inetpub\\edgeguard\\dp-keys");

        // ── CORS ─────────────────────────────────────────────────────────────
        var corsOrigins = app.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:4200", "http://localhost:5173"];

        logger.LogInformation(
            "CORS AllowedOrigins ({Count}): {Origins}",
            corsOrigins.Length,
            string.Join(" | ", corsOrigins));

        if (corsOrigins.All(o =>
            o.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
            o.StartsWith("https://localhost", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning(
                "CORS is restricted to localhost origins — requests from external IPs will be rejected with 405/CORS error. " +
                "Add the SPA origin to Cors:AllowedOrigins in appsettings or as an env var.");
        }

        // ── SPA mode ─────────────────────────────────────────────────────────
        if (env.IsDevelopment())
        {
            logger.LogInformation(
                "SPA mode=Development — Angular dev-server expected at CORS origins; " +
                "static files NOT served from wwwroot; MapSpaFallback NOT registered.");
        }
        else
        {
            var wwwroot   = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            var indexHtml = Path.Combine(wwwroot, "index.html");
            var spaBuilt  = File.Exists(indexHtml);

            if (spaBuilt)
            {
                var jsCount  = Directory.GetFiles(wwwroot, "*.js",  SearchOption.AllDirectories).Length;
                var cssCount = Directory.GetFiles(wwwroot, "*.css", SearchOption.AllDirectories).Length;
                logger.LogInformation(
                    "SPA mode=Production — Angular bundle present. " +
                    "wwwroot={Wwwroot} js={Js} css={Css}",
                    wwwroot, jsCount, cssCount);
            }
            else
            {
                logger.LogWarning(
                    "SPA mode=Production but wwwroot/index.html NOT FOUND — " +
                    "run 'ng build --configuration production' and copy dist output to wwwroot. " +
                    "Path checked: {Path}", indexHtml);
            }

            logger.LogInformation(
                "SPA fallback exclusions (these prefixes return 404 instead of index.html): {Prefixes}",
                string.Join(", ", SpaProductionExtensions.BackendPrefixes));
        }

        // ── HTTPS redirection ────────────────────────────────────────────────
        var httpsRedirect = app.Configuration.GetValue("UseHttpsRedirection", false);
        logger.LogInformation("UseHttpsRedirection={Enabled}", httpsRedirect);

        // ── Auth ─────────────────────────────────────────────────────────────
        // JWT uses Jwt:SecretKey (HMAC symmetric) — NOT an Authority/OIDC endpoint.
        var jwtSecretKey = app.Configuration["Jwt:SecretKey"];
        var jwtIssuer    = app.Configuration["Jwt:Issuer"]   ?? "(default)";
        var jwtAudience  = app.Configuration["Jwt:Audience"] ?? "(default)";

        if (!string.IsNullOrWhiteSpace(jwtSecretKey))
        {
            var isDefaultKey = jwtSecretKey.StartsWith("DEV-ONLY", StringComparison.OrdinalIgnoreCase);
            if (isDefaultKey)
                logger.LogWarning(
                    "JWT Jwt:SecretKey is set to the DEV-ONLY placeholder — " +
                    "override it in appsettings.Production.json or via env var Jwt__SecretKey before going live.");
            else
                logger.LogInformation(
                    "JWT configured — Issuer={Issuer} Audience={Audience} KeyLength={Len}",
                    jwtIssuer, jwtAudience, jwtSecretKey.Length);
        }
        else
        {
            logger.LogWarning(
                "Jwt:SecretKey is missing — AddEdgeAuthentication() will throw on startup. " +
                "Set it in appsettings.Production.json or via env var Jwt__SecretKey.");
        }

        // ── Database connection (sanitized — no credentials) ─────────────────
        var connStr = app.Configuration.GetConnectionString("HubDatabase");
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            var sanitized = SanitizeConnectionString(connStr);
            logger.LogInformation("Database={Connection}", sanitized);
        }
        else
        {
            logger.LogWarning("ConnectionStrings:HubDatabase not found");
        }

        // ── Registered API endpoints — listed ONLY after all Map* calls ─────
        // This verifies controller discovery. If an endpoint is missing here
        // it will NEVER be reachable regardless of client request method.
        // Read the route builder's OWN data sources, not the EndpointDataSource in DI.
        // The DI one is a CompositeEndpointDataSource fed by data sources registered in
        // the container; the ones created by MapControllers/MapGet/MapHub live in
        // IEndpointRouteBuilder.DataSources and are only merged in when the pipeline
        // starts. Since this runs before app.Run(), resolving from DI returned an EMPTY
        // composite — which made the api/auth/login check below fire on every single
        // deployment regardless of whether the controller was registered.
        var dataSources = ((IEndpointRouteBuilder)app).DataSources;
        if (dataSources.Count > 0)
        {
            var endpointDataSource = new CompositeEndpointDataSource(dataSources);

            var apiEndpoints = endpointDataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Where(e => e.RoutePattern.RawText is not null)
                .OrderBy(e => e.RoutePattern.RawText)
                .Select(e =>
                {
                    var methods = e.Metadata
                        .GetOrderedMetadata<HttpMethodMetadata>()
                        .SelectMany(m => m.HttpMethods)
                        .Distinct()
                        .ToArray();
                    return (Pattern: e.RoutePattern.RawText!, Methods: methods);
                })
                .ToList();

            var apiOnly  = apiEndpoints.Where(e => e.Pattern.StartsWith("api/",  StringComparison.OrdinalIgnoreCase)).ToList();
            var hubsOnly = apiEndpoints.Where(e => e.Pattern.StartsWith("hubs/", StringComparison.OrdinalIgnoreCase) ||
                                                   e.Pattern.StartsWith("hub/",  StringComparison.OrdinalIgnoreCase)).ToList();

            logger.LogInformation(
                "Registered endpoints — API={ApiCount} SignalR={HubCount} Total={Total}",
                apiOnly.Count, hubsOnly.Count, apiEndpoints.Count);

            foreach (var (pattern, methods) in apiOnly)
            {
                var methodList = methods.Length > 0 ? string.Join("|", methods) : "ANY";
                logger.LogInformation("  [{Methods}] /{Pattern}", methodList, pattern);
            }

            // Verify auth endpoint is present — critical for SPA login
            var hasAuthLogin = apiOnly.Any(e =>
                e.Pattern.Contains("auth/login", StringComparison.OrdinalIgnoreCase));

            if (!hasAuthLogin)
                logger.LogWarning(
                    "ENDPOINT NOT REGISTERED: api/auth/login — controller discovery failed or " +
                    "IAuthenticationService DI registration is missing. " +
                    "All login requests will fall through to the 404 fallback.");
            else
                logger.LogInformation("auth/login endpoint: OK (POST)");
        }
        else
        {
            logger.LogWarning("No endpoint data sources registered — cannot verify registered routes.");
        }

        // ── SignalR hub ───────────────────────────────────────────────────────
        logger.LogInformation("SignalR hub mapped at /hubs/notifications");

        logger.LogInformation("=== Hub startup diagnostics complete ===");

        return app;
    }

    /// <summary>
    /// Removes password/credentials from a Postgres or generic ADO connection string.
    /// </summary>
    private static string SanitizeConnectionString(string raw)
    {
        // Handle key=value style (Postgres, SqlServer, etc.)
        var parts = raw.Split(';', StringSplitOptions.RemoveEmptyEntries);
        var safe = parts
            .Where(p =>
            {
                var key = p.Split('=')[0].Trim().ToLowerInvariant();
                return key is not ("password" or "pwd" or "user id" or "uid"
                    or "username" or "user" or "secret");
            })
            .ToArray();
        return string.Join("; ", safe);
    }
}
