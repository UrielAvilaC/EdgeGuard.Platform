using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Api.Extensions;

/// <summary>
/// Configures the ASP.NET Core pipeline to serve the Angular SPA
/// from the <c>wwwroot</c> directory.
/// <para>
/// Only called in non-Development environments. In development, the Angular
/// dev server runs independently (ng serve) and is proxied via the CORS policy.
/// </para>
/// <remarks>
/// Middleware order matters:
/// <list type="number">
///   <item><see cref="UseSpaStaticFiles"/> → UseDefaultFiles + UseStaticFiles.</item>
///   <item><see cref="MapSpaFallback"/> → explicit 404 for backend prefixes,
///         then MapFallbackToFile for every other unknown route.</item>
/// </list>
/// The fallback must be registered AFTER <see cref="Microsoft.AspNetCore.Builder.ControllerEndpointRouteBuilderExtensions.MapControllers"/>
/// and <see cref="Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions.MapHub{THub}"/>
/// so API routes and SignalR hubs take priority.
/// </remarks>
/// </summary>
public static class SpaProductionExtensions
{
    /// <summary>
    /// Backend path prefixes that must NEVER be served as Angular routes.
    /// Requests matching these prefixes that reach the fallback get a 404.
    /// </summary>
    internal static readonly string[] BackendPrefixes =
    [
        "/api/",
        "/hubs/",
        "/health",
        "/openapi",
        "/metrics",
        "/swagger"
    ];

    /// <summary>
    /// Adds <c>UseDefaultFiles</c> and <c>UseStaticFiles</c> middleware to serve
    /// the Angular SPA bundle from <c>wwwroot</c>.
    /// Logs startup details: whether <c>index.html</c> exists, how many static
    /// assets were found, and which cache headers are applied.
    /// </summary>
    public static IApplicationBuilder UseSpaStaticFiles(this IApplicationBuilder app)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SpaStaticFiles");

        var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
        var wwwroot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

        // ── Verify wwwroot contents ───────────────────────────────────────────
        var indexHtml  = Path.Combine(wwwroot, "index.html");
        var assetsDir  = Path.Combine(wwwroot, "assets");
        var jsFiles    = Directory.Exists(wwwroot)
            ? Directory.GetFiles(wwwroot, "*.js", SearchOption.AllDirectories).Length
            : 0;
        var cssFiles   = Directory.Exists(wwwroot)
            ? Directory.GetFiles(wwwroot, "*.css", SearchOption.AllDirectories).Length
            : 0;

        if (File.Exists(indexHtml))
        {
            var indexSize = new FileInfo(indexHtml).Length;
            logger.LogInformation(
                "SPA static files: wwwroot={Wwwroot} index.html={IndexSize}B js={Js} css={Css} assets={HasAssets}",
                wwwroot, indexSize, jsFiles, cssFiles, Directory.Exists(assetsDir));
        }
        else
        {
            logger.LogWarning(
                "SPA wwwroot/index.html NOT FOUND at {Path} — Angular bundle missing. " +
                "Run 'ng build --configuration production' and copy the dist output to wwwroot.",
                indexHtml);
        }

        // ── Log excluded backend prefixes ─────────────────────────────────────
        logger.LogInformation(
            "SPA static files active — backend prefixes excluded from fallback: {Prefixes}",
            string.Join(", ", BackendPrefixes));

        // ── Wire middleware ───────────────────────────────────────────────────
        // UseDefaultFiles MUST come before UseStaticFiles so "/" → "/index.html".
        app.UseDefaultFiles();

        app.UseStaticFiles(new StaticFileOptions
        {
            // Hashed assets (main.abc123.js) can be cached indefinitely.
            // Non-hashed files (index.html, assets/) use no-cache so Angular
            // re-fetches the bootstrapper after a deploy.
            OnPrepareResponse = ctx =>
            {
                var fileName = ctx.File.Name;
                var isHashedAsset = IsHashedAsset(fileName);
                var headers = ctx.Context.Response.Headers;

                if (isHashedAsset)
                {
                    headers.CacheControl = "public, max-age=31536000, immutable";
                }
                else
                {
                    headers.CacheControl = "no-cache, no-store, must-revalidate";
                    headers.Pragma       = "no-cache";
                    headers.Expires      = "0";
                }

                logger.LogDebug(
                    "Static file served: {File} Cached={Cached}",
                    fileName, isHashedAsset);
            }
        });

        return app;
    }

    /// <summary>
    /// Registers the SPA fallback route with explicit backend exclusions.
    /// Uses a single pattern-less <c>MapFallback</c> (true last-resort, never
    /// competes with controller or hub routes) and filters inside the handler:
    /// <list type="bullet">
    ///   <item>Paths starting with <see cref="BackendPrefixes"/> → 404 JSON (never serve index.html).</item>
    ///   <item>All other unmatched paths → <c>wwwroot/index.html</c> for Angular routing.</item>
    /// </list>
    /// </summary>
    public static IEndpointRouteBuilder MapSpaFallback(this IEndpointRouteBuilder endpoints)
    {
        var env = endpoints.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = endpoints.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("SpaFallback");

        var indexPath = Path.Combine(
            env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"),
            "index.html");

        // Capture EndpointDataSource so we can detect 405 (path exists, method wrong)
        // inside the fallback delegate. This is set AFTER MapControllers() so it's complete.
        var endpointDataSource = endpoints.ServiceProvider.GetRequiredService<EndpointDataSource>();

        logger.LogInformation(
            "SPA fallback registered (single catch-all) — {Count} backend prefixes return 404: {Prefixes}",
            BackendPrefixes.Length,
            string.Join(", ", BackendPrefixes));

        endpoints.MapFallback(async context =>
        {
            var path   = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            var isBackendPrefix = BackendPrefixes.Any(
                p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (isBackendPrefix)
            {
                // Check if the path IS registered but with a different HTTP method (405 scenario).
                // This happens when the client sends GET to a POST-only endpoint (e.g. browser navigation).
                // Normally ASP.NET Core returns 405 before reaching MapFallback, but under IIS
                // in-process this can occasionally fall through.
                var normalizedPath = path.TrimStart('/');
                var pathCandidates = endpointDataSource.Endpoints
                    .OfType<RouteEndpoint>()
                    .Where(e => string.Equals(
                        e.RoutePattern.RawText?.TrimStart('/'),
                        normalizedPath,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (pathCandidates.Count > 0)
                {
                    var allowedMethods = pathCandidates
                        .SelectMany(e => e.Metadata
                            .GetOrderedMetadata<HttpMethodMetadata>()
                            .SelectMany(m => m.HttpMethods))
                        .Distinct()
                        .ToArray();

                    logger.LogWarning(
                        "SPA fallback: path exists but method mismatch — " +
                        "Path={Path} ReceivedMethod={Method} AllowedMethods={Allowed}. " +
                        "Browser navigation (GET) cannot call POST-only API endpoints.",
                        path, method, string.Join(", ", allowedMethods));

                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    context.Response.Headers.Allow = string.Join(", ", allowedMethods);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        $"{{\"error\":\"Method not allowed\"," +
                        $"\"path\":\"{path}\"," +
                        $"\"method\":\"{method}\"," +
                        $"\"allowedMethods\":[{string.Join(",", allowedMethods.Select(m => $"\"{m}\""))}]," +
                        $"\"hint\":\"Browser navigation sends GET. Use an HTTP client (Angular, curl, Postman) to send {string.Join("/", allowedMethods)}.\"}}");
                    return;
                }

                logger.LogWarning(
                    "SPA fallback: backend-prefixed path with no matching endpoint — returning 404. " +
                    "Path={Path} Method={Method}",
                    path, method);

                context.Response.StatusCode  = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $"{{\"error\":\"Not found\",\"path\":\"{path}\"}}");
                return;
            }

            // Angular client-side route — serve index.html
            logger.LogDebug("SPA fallback: serving index.html for Angular route {Path}", path);

            if (!File.Exists(indexPath))
            {
                logger.LogError("SPA index.html not found at {IndexPath}", indexPath);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("SPA bundle not deployed.");
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            await context.Response.SendFileAsync(indexPath);
        });

        return endpoints;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> for Angular hashed chunk files (e.g. <c>main.abc1234.js</c>).
    /// These files get an immutable cache header because their names change on every build.
    /// </summary>
    private static bool IsHashedAsset(string fileName)
    {
        // Angular CLI naming: name.HASH.ext — the hash segment is 8-20 hex chars.
        var dotParts = fileName.Split('.');
        if (dotParts.Length < 3) return false;

        // Second-to-last part should be a hex hash
        var candidate = dotParts[^2];
        return candidate.Length is >= 8 and <= 20
            && candidate.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F'));
    }
}

