using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Dicom.Edge.Common.Resilience;
using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Api.Middleware;
using Dicom.Edge.Hub.Application.Extensions;
using Dicom.Edge.Hub.Diagnostics.Extensions;
using Dicom.Edge.Hub.Infrastructure.Extensions;
using Dicom.Edge.Hub.Persistence.Configuration;
using Dicom.Edge.Hub.Persistence.Extensions;
using Dicom.Edge.Hub.Api.Extensions;
using Dicom.Edge.Hub.Api.Hubs;
using Dicom.Edge.Hub.Api.HostedServices;
using Dicom.Edge.Security.Extensions;

BootstrapLogger.Initialize(HubApiConstants.BootstrapLogPath);

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Enterprise Serilog logging (file, Seq, HTTP, console, PHI redaction)
    builder.UseHubLogging();

    // P0-8: Hosted service failures must NOT take down the host.
    // Individual services (HL7 listener, dispatch workers) implement their own supervisor loops.
    builder.Services.Configure<HostOptions>(o =>
        o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();

    // CORS policy — allow SPA origins configured via appsettings or env vars
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:4200", "http://localhost:5173"];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // P0-9: Rate limiting — protect edge and API endpoints from abuse.
    // The "edge" policy is PARTITIONED BY node identity (X-Node-Id header)
    // so one chatty node cannot exhaust the bucket for the others.
    // The "api" policy is global (sufficient for SPA users).
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Per-node partitioned limiter for /api/edge/* endpoints.
        options.AddPolicy("edge", context =>
        {
            var partitionKey = context.Request.Headers["X-Node-Id"].ToString();
            if (string.IsNullOrEmpty(partitionKey))
                partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit          = 200,
                    Window               = TimeSpan.FromMinutes(1),
                    QueueLimit           = 20,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
        });

        // Global limiter for /api/* endpoints (SPA-facing).
        options.AddFixedWindowLimiter("api", limiter =>
        {
            limiter.PermitLimit          = 200;
            limiter.Window               = TimeSpan.FromMinutes(1);
            limiter.QueueLimit           = 20;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    });

    // Resolve connection string: environment variable takes precedence, then appsettings
    var connectionString =
        Environment.GetEnvironmentVariable(HubApiConstants.ConnectionStringEnvVar)
        ?? builder.Configuration.GetConnectionString(HubApiConstants.ConnectionStringName)
        ?? throw new InvalidOperationException(
            $"Set environment variable '{HubApiConstants.ConnectionStringEnvVar}' " +
            $"or configure ConnectionStrings:{HubApiConstants.ConnectionStringName}.");

    // Inject connection string so downstream code finds it via IConfiguration
    builder.Configuration[$"ConnectionStrings:{HubApiConstants.ConnectionStringName}"] = connectionString;

    // Load operational settings from the database (overrides appsettings)
    builder.Configuration.AddHubDatabaseConfiguration(connectionString);

    // Enterprise diagnostics (PHI redaction, audit, health checks, OTel)
    builder.Services.AddHubDiagnostics(builder.Configuration);

    // Platform resilience pipelines (retry + circuit breaker via Polly v8)
    builder.Services.AddPlatformResilience(builder.Configuration);

    // P0-10: Per-node resilience — one circuit breaker per nodeId.
    // Prevents one unreachable node from tripping the breaker for all others.
    builder.Services.AddPerNodeResilience(builder.Configuration);

    // Clean Architecture service registration
    builder.Services.AddHubPersistence(builder.Configuration);
    builder.Services.AddHubDomainServices();
    builder.Services.AddHubApplication(builder.Configuration);
    builder.Services.AddWhatsAppServices();
    builder.Services.AddHl7Infrastructure();
    builder.Services.AddHubHostedServices(builder.Configuration);

    // JWT authentication + permission-based authorization pipeline
    builder.Services.AddEdgeSecurity(builder.Configuration);
    builder.Services.AddEdgeAuthentication(builder.Configuration);

    // HTML sanitizer for rendering ORU report bodies safely (XSS protection).
    builder.Services.AddSingleton<Ganss.Xss.IHtmlSanitizer>(_ => new Ganss.Xss.HtmlSanitizer());

    // SignalR for real-time dashboard notifications
    builder.Services.AddSignalR();

    // P1-1: background dispatcher that drains the node push queue off the request
    // path and reports results over SignalR. Lives here because it needs IHubContext.
    builder.Services.AddHostedService<NodePushDispatchHostedService>();

    var app = builder.Build();

    // ── Apply pending migrations & seed system settings ───────────────────
    await app.Services.MigrateHubAsync();
    await app.Services.SeedHubSettingsAsync();
    await app.Services.SeedAdminUserAsync();

    // Diagnostics middleware pipeline (order matters)
    app.UseCorrelationId();
    app.UsePlatformExceptionHandling();
    app.MapDiagnosticsEndpoints();

    // OpenAPI available in all environments for enterprise tooling
    app.MapOpenApi();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "EdgeGuard Hub API v1");
        });
    }

    app.UseCors();
    //app.UseHttpsRedirection();
    app.UseRateLimiter();

    // Serve Angular SPA from wwwroot only in non-Development environments.
    // In development the Angular CLI dev-server runs separately (ng serve).
    if (!app.Environment.IsDevelopment())
        app.UseSpaStaticFiles();

    // Bootstrap token validation for /edge/register (before auth pipeline)
    app.UseMiddleware<BootstrapTokenMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    // SignalR uses long-lived connections; a per-request rate limiter would break
    // the persistent hub channel, so it is explicitly excluded.
    app.MapHub<EdgeHubNotificationHub>("/hubs/notifications").DisableRateLimiting();

    // SPA client-side routing fallback — must be last, only in production.
    if (!app.Environment.IsDevelopment())
        app.MapSpaFallback();

    // ── Startup diagnostics — called AFTER all Map* so endpoint list is complete ──
    app.LogStartupDiagnostics();

    app.Run();
}
catch (Exception ex)
{
    BootstrapLogger.FatalShutdown(ex);
    throw;
}
finally
{
    BootstrapLogger.CloseAndFlush();
}
