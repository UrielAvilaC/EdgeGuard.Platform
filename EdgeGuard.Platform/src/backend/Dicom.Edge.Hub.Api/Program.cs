using Dicom.Edge.Diagnostics.Bootstrap;
using Dicom.Edge.Diagnostics.Extensions;
using Dicom.Edge.Hub.Api.Constants;
using Dicom.Edge.Hub.Application.Extensions;
using Dicom.Edge.Hub.Diagnostics.Extensions;
using Dicom.Edge.Hub.Infrastructure.Extensions;

BootstrapLogger.Initialize(HubApiConstants.BootstrapLogPath);

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Enterprise Serilog logging (file, Seq, HTTP, console, PHI redaction)
    builder.UseHubLogging();

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddProblemDetails();

    // Validate required connection string (fail-fast)
    _ = builder.Configuration.GetConnectionString(HubApiConstants.ConnectionStringName)
        ?? throw new InvalidOperationException(
            HubApiConstants.MissingConnectionStringMessage);

    // Enterprise diagnostics (PHI redaction, audit, health checks, OTel)
    builder.Services.AddHubDiagnostics(builder.Configuration);

    // Clean Architecture service registration
    builder.Services.AddHubPersistence(builder.Configuration);
    builder.Services.AddHubDomainServices();
    builder.Services.AddHubApplication(builder.Configuration);
    builder.Services.AddHl7Infrastructure();
    builder.Services.AddHubHostedServices(builder.Configuration);

    var app = builder.Build();

    // Diagnostics middleware pipeline (order matters)
    app.UseCorrelationId();
    app.UsePlatformExceptionHandling();
    app.MapDiagnosticsEndpoints();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
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
