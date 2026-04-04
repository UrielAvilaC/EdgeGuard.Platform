using Dicom.Edge.Node;
using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Node.Persistence.Extensions;
using Dicom.Edge.Node.Storage.Extensions;

BootstrapLogger.Initialize("logs/node-bootstrap-.log");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.diagnostics.json", optional: true, reloadOnChange: true);

    builder.UseEdgeLogging(builder.Configuration);
    builder.Services.AddEdgeDiagnostics(builder.Configuration);
    builder.Services.AddEdgePersistence(builder.Configuration);
    builder.Services.AddEdgeStorage();

    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
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
