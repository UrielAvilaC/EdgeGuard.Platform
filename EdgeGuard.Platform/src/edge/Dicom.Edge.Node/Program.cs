using Dicom.Edge.Node;
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

BootstrapLogger.Initialize("logs/node-bootstrap-.log");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.diagnostics.json", optional: true, reloadOnChange: true);

    // ── Resolve SQLite DB path (env var takes precedence) ────────────────
    var dbPath = Environment.GetEnvironmentVariable(PersistenceExtensions.NodeDbPathEnvVar)
        ?? builder.Configuration["Persistence:DatabasePath"]
        ?? "edge-node.db";
    var sqliteConnectionString = $"Data Source={dbPath}";

    // ── Load operational settings from SQLite database ────────────────────
    builder.Configuration.AddNodeDatabaseConfiguration(sqliteConnectionString);

    // ── Kestrel configuration for Node API endpoints ─────────────────────
    var nodeApiPort = builder.Configuration.GetValue("NodeApi:Port", 5120);
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(nodeApiPort);
    });

    // ── Diagnostics (logging, OpenTelemetry, health checks) ──────────────
    builder.Host.UseEdgeLogging(builder.Configuration);
    builder.Services.AddEdgeDiagnostics(builder.Configuration);

    // ── Persistence (SQLite EF Core, repos, settings, cleanup) ───────────
    builder.Services.AddEdgePersistence(builder.Configuration);

    // ── Storage (local file system, DICOM file writer) ───────────────────
    builder.Services.AddEdgeStorage();

    // ── Hub Configuration Sync (HTTP client, config pull, heartbeat) ─────
    builder.Services.AddNodeConfiguration(builder.Configuration);

    // ── Work Queue (INodeWorkQueue placeholder / extensions) ─────────────
    builder.Services.AddNodeQueue();

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
