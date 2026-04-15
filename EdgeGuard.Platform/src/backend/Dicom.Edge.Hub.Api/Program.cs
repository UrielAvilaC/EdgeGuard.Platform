using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Dicom.Edge.Common.Resilience;
using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Application.Extensions;
using Dicom.Edge.Hub.Diagnostics.Extensions;
using Dicom.Edge.Hub.Infrastructure.Extensions;
using Dicom.Edge.Hub.Persistence.Configuration;
using Dicom.Edge.Hub.Persistence.Extensions;

BootstrapLogger.Initialize(HubApiConstants.BootstrapLogPath);

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Enterprise Serilog logging (file, Seq, HTTP, console, PHI redaction)
    builder.UseHubLogging();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();

    // CORS policy — allow SPA origins configured via appsettings or env vars
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:3000", "http://localhost:5173"];
            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Rate limiting — protect edge and API endpoints from abuse
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter("edge", limiter =>
        {
            limiter.PermitLimit = 100;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 10;
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        options.AddFixedWindowLimiter("api", limiter =>
        {
            limiter.PermitLimit = 200;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 20;
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

    // Clean Architecture service registration
    builder.Services.AddHubPersistence(builder.Configuration);
    builder.Services.AddHubDomainServices();
    builder.Services.AddHubApplication(builder.Configuration);
    builder.Services.AddHl7Infrastructure();
    builder.Services.AddHubHostedServices(builder.Configuration);

    var app = builder.Build();

    // ── Apply pending migrations & seed system settings ───────────────────
    await app.Services.MigrateHubAsync();
    await app.Services.SeedHubSettingsAsync();

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

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers();

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
