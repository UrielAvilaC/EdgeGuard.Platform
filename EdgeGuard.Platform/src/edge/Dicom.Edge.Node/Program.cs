using Dicom.Edge.Node;
using Dicom.Edge.Common.Resilience;
using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Node.Api;
using Dicom.Edge.Node.Configuration;
using Dicom.Edge.Node.DicomServer;
using Dicom.Edge.Node.Persistence.Configuration;
using Dicom.Edge.Node.Persistence.Extensions;
using Dicom.Edge.Node.Processing;
using Dicom.Edge.Node.Queue;
using Dicom.Edge.Node.Router;
using Dicom.Edge.Node.Sender;
using Dicom.Edge.Node.Storage.Extensions;
using Dicom.Edge.Node.Worklist;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

BootstrapLogger.Initialize(NodeConstants.BootstrapLogPath);

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile(NodeConstants.DiagnosticsSettingsFile, optional: true, reloadOnChange: true);

    // ── Resolve SQLite connection string (env var → ConnectionStrings → default) ─
    var sqliteConnectionString = Environment.GetEnvironmentVariable(PersistenceExtensions.NodeDbPathEnvVar)
                                     is { Length: > 0 } envPath
        ? $"Data Source={envPath}"
        : builder.Configuration.GetConnectionString(NodeConstants.ConnectionStringName)
          ?? NodeConstants.DefaultConnectionString;

    // ── Load operational settings from SQLite database ────────────────────
    builder.Configuration.AddNodeDatabaseConfiguration(sqliteConnectionString);

    // ── Kestrel configuration for Node API endpoints ─────────────────────
    var nodeApiPort = builder.Configuration.GetValue(NodeConstants.NodeApiPortKey, NodeConstants.DefaultNodeApiPort);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(nodeApiPort);
    });

    // ── OpenAPI (document generation for Swagger UI) ─────────────────────
    builder.Services.AddOpenApi();

    // ── Diagnostics (logging, OpenTelemetry, health checks) ──────────────
    builder.Host.UseEdgeLogging(builder.Configuration);
    builder.Services.AddEdgeDiagnostics(builder.Configuration);

    // ── Platform resilience pipelines (retry + circuit breaker via Polly v8) ─
    builder.Services.AddPlatformResilience(builder.Configuration);

    // ── Persistence (SQLite EF Core, repos, settings, cleanup) ───────────
    builder.Services.AddEdgePersistence(builder.Configuration);

    // ── Storage (local file system, DICOM file writer) ───────────────────
    builder.Services.AddEdgeStorage();

    // ── Hub Configuration Sync (HTTP client, config pull, heartbeat) ─────
    builder.Services.AddNodeConfiguration(builder.Configuration);

    // ── Work Queue (INodeWorkQueue placeholder / extensions) ─────────────
    builder.Services.AddNodeQueue();

    // ── DICOM Instance Handler (C-STORE callback → save + enqueue) ─────
    builder.Services.AddSingleton<IDicomInstanceHandler, DicomInstanceHandler>();

    // ── DICOM Server (C-STORE SCP + MWL C-FIND SCP) ─────────────────────
    builder.Services.AddNodeDicomServer(builder.Configuration);

    // ── PACS Sender (C-STORE SCU) ────────────────────────────────────────
    builder.Services.AddNodeSender(builder.Configuration);

    // ── Study Router (rule-based routing engine) ─────────────────────────
    builder.Services.AddNodeRouter();

    // ── Study Processing Pipeline (dequeue → route → send) ──────────────
    builder.Services.AddNodeProcessing();

    // ── Worklist (in-memory HL7 worklist manager) ────────────────────────
    builder.Services.AddNodeWorklist();

    // ── Node API (controllers for Hub→Node HTTP push) ────────────────────
    builder.Services.AddNodeApi();

    // ── Worker (lifecycle orchestrator) ──────────────────────────────────
    builder.Services.AddHostedService<Worker>();

    var app = builder.Build();

    // ── Diagnostics middleware pipeline (order matters) ───────────────────
    app.UseCorrelationId();
    app.UsePlatformExceptionHandling();
    app.MapDiagnosticsEndpoints();

    // ── OpenAPI & Swagger UI (development only) ──────────────────────────
    app.MapOpenApi();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "EdgeGuard Node API v1");
        });
    }

    // ── Map Node API endpoints ───────────────────────────────────────────
    app.MapNodeApi();

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
