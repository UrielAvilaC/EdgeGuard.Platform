using Dicom.Edge.Node;
using Dicom.Edge.Common.Resilience;
using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Diagnostics.HealthChecks;
using Dicom.Edge.Node.Api;
using Dicom.Edge.Node.Configuration;
using Dicom.Edge.Node.DicomServer;
using Dicom.Edge.Node.Persistence.Configuration;
using Dicom.Edge.Node.Persistence.Extensions;
using Dicom.Edge.Node.Persistence.Storage;
using Dicom.Edge.Node.Processing;
using Dicom.Edge.Node.Queue;
using Dicom.Edge.Node.Router;
using Dicom.Edge.Node.Sender;
using Dicom.Edge.Node.Storage.Extensions;
using Dicom.Edge.Node.Worklist;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

BootstrapLogger.Initialize(NodeConstants.BootstrapLogPath);

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Windows Service support (Production only) ────────────────────────
    if (builder.Environment.IsProduction())
    {
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = "EdgeGuardNode";
        });
    }

    builder.Configuration.AddJsonFile(NodeConstants.DiagnosticsSettingsFile, optional: true, reloadOnChange: true);

    // ── Resolve SQLite connection string (env var → ConnectionStrings → default) ─
    var sqliteConnectionString = Environment.GetEnvironmentVariable(PersistenceExtensions.NodeDbPathEnvVar)
                                     is { Length: > 0 } envPath
        ? $"Data Source={envPath}"
        : builder.Configuration.GetConnectionString(NodeConstants.ConnectionStringName)
          ?? NodeConstants.DefaultConnectionString;

    // ── Las rutas de base y de DICOM no pueden solaparse ──────────────────
    // Hay dos candidatas a raíz efectiva y se comprueban las dos, porque cuál gana
    // depende de algo que aquí todavía no se sabe: la fila storage.root_path manda en un
    // nodo ya registrado, y el valor desplegado (NodeStorage:RootPath) manda mientras no
    // lo esté, porque NodeSettingsDeploymentHydrator lo reescribe en cada arranque. Como
    // esto corre antes de construir el host —y antes de que exista la base en una
    // instalación nueva—, validar solo una dejaría pasar el caso real la mitad de las
    // veces. Rechazar si CUALQUIERA de las dos se solapa es lo conservador y lo correcto.
    var storageRootCandidates = new[]
        {
            NodeStorageRoot.ReadConfigured(sqliteConnectionString),
            builder.Configuration.GetValue<string>(NodeStorageRoot.DeployedConfigPath),
        }
        .Where(v => !string.IsNullOrWhiteSpace(v))
        .Select(v => NodeStorageRoot.Resolve(v, builder.Environment.ContentRootPath))
        .DefaultIfEmpty(NodeStorageRoot.Resolve(null, builder.Environment.ContentRootPath))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    var dicomRootPath = storageRootCandidates[0];

    if (storageRootCandidates
            .Select(root => StoragePathGuard.Validate(sqliteConnectionString, root))
            .FirstOrDefault(error => error is not null) is { } pathError)
    {
        // En desarrollo se avisa y se sigue; en producción es motivo de no arrancar,
        // porque el daño (purga de estudios) es silencioso y acumulativo.
        if (builder.Environment.IsProduction())
            throw new InvalidOperationException(pathError);

        Console.Error.WriteLine("ADVERTENCIA: " + pathError);
    }

    // ── Load operational settings from SQLite database ────────────────────
    builder.Configuration.AddNodeDatabaseConfiguration(sqliteConnectionString, builder.Services);

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

    // El chequeo de almacenamiento vigila la raíz DICOM efectiva, no una ruta fija de
    // appsettings: en el nodo la decide storage.root_path y el Hub puede moverla.
    builder.Services.Replace(
        ServiceDescriptor.Singleton<IStorageHealthLocation, NodeStorageHealthLocation>());

    // ── Hub Configuration Sync (HTTP client, config pull, heartbeat) ─────
    builder.Services.AddNodeConfiguration(builder.Configuration);

    // ── Work Queue (INodeWorkQueue — backed by Persistence layer) ─────────
    builder.Services.AddNodeQueue();

    // ── DICOM Instance Handler (C-STORE callback → save + enqueue) ─────
    builder.Services.AddSingleton<IDicomInstanceHandler, DicomInstanceHandler>();

    // ── DICOM Association Tracker (per-association audit + metrics) ──────
    builder.Services.AddSingleton<IDicomAssociationTracker, DicomAssociationTracker>();
    // ── Study Root C-FIND handler (Query/Retrieve) ───────────────────────
    builder.Services.AddSingleton<IStudyRootCFindHandler, StudyRootCFindHandler>();

    // ── DICOM Server (C-STORE SCP + MWL C-FIND SCP) ─────────────────────
    builder.Services.AddNodeDicomServer(builder.Configuration);

    // ── PACS Sender (C-STORE SCU) ────────────────────────────────────────
    builder.Services.AddNodeSender(builder.Configuration);

    // ── Study Router (rule-based routing engine) ─────────────────────────
    builder.Services.AddNodeRouter();

    // ── Study Processing Pipeline (dequeue → route → send) ──────────────
    builder.Services.AddNodeProcessing();

    // ── Worklist (SQLite-backed, fallback to in-memory) ───────────────────
    builder.Services.AddNodeWorklist();

    // ── Node API (controllers for Hub→Node HTTP push) ────────────────────
    builder.Services.AddNodeApi();

    // ── Worker (lifecycle orchestrator) ──────────────────────────────────
    builder.Services.AddHostedService<Worker>();

    var app = builder.Build();

    // Las dos rutas de datos, resueltas, en el log de arranque. Son la parte del
    // despliegue más fácil de dar por supuesta y más difícil de verificar sin entrar al
    // servidor — y la raíz DICOM se configura por base, no por appsettings.
    app.Logger.LogInformation(
        "Rutas del nodo — base de datos: {DbPath} | almacenamiento DICOM: {DicomRoot}",
        StoragePathGuard.DatabaseFile(sqliteConnectionString) ?? "(en memoria)",
        dicomRootPath);

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

    // P0-1: Validate HMAC-signed Hub→Node configuration pushes.
    // Feature-flagged via NodeAuth:Enforce (default false for safe rollout).
    app.UseMiddleware<Dicom.Edge.Node.Api.Middleware.HubAuthenticationMiddleware>();

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
