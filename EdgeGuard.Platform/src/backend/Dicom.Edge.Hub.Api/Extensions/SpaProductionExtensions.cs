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
///   <item><see cref="UseDefaultFiles"/> rewrites "/" → "/index.html" before static files.</item>
///   <item><see cref="UseStaticFiles"/> serves hashed JS/CSS/asset bundles from wwwroot.</item>
///   <item><see cref="MapFallbackToFile"/> catches unknown routes and returns index.html
///         so Angular's client-side router can handle them.</item>
/// </list>
/// The fallback must be registered AFTER <see cref="MapControllers"/> and
/// <see cref="MapHub{THub}"/> so API routes and SignalR 
/// s take priority.
/// </remarks>
/// </summary>
public static class SpaProductionExtensions
{
    /// <summary>
    /// Adds <c>UseDefaultFiles</c> and <c>UseStaticFiles</c> middleware to serve
    /// the Angular SPA bundle from <c>wwwroot</c>.
    /// Call this before <c>UseAuthentication</c> / <c>UseAuthorization</c> so static
    /// assets are served without requiring a JWT.
    /// </summary>
    public static IApplicationBuilder UseSpaStaticFiles(this IApplicationBuilder app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
        return app;
    }

    /// <summary>
    /// Registers the SPA fallback route. Any request that does not match an API
    /// controller, hub, or static file is answered with <c>wwwroot/index.html</c>,
    /// allowing Angular's router to take over.
    /// Call this as the last endpoint mapping in the pipeline.
    /// </summary>
    public static IEndpointRouteBuilder MapSpaFallback(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFallbackToFile("index.html");
        return endpoints;
    }
}
