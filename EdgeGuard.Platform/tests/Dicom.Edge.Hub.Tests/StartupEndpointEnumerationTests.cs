using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dicom.Edge.Hub.Tests;

/// <summary>
/// Regression guard for <c>StartupDiagnosticsExtensions.LogStartupDiagnostics</c>.
/// <para>
/// That block enumerates registered endpoints so an operator can see, at startup, whether
/// controller discovery worked. It used to read <c>EndpointDataSource</c> from DI before
/// <c>app.Run()</c>, which returns a composite fed only by data sources registered in the
/// container — empty at that point. The result was a permanent false positive:
/// "ENDPOINT NOT REGISTERED: api/auth/login" on every deployment, including healthy ones.
/// </para>
/// <para>
/// These tests pin the two halves of that finding so the fix cannot be quietly undone.
/// </para>
/// </summary>
public class StartupEndpointEnumerationTests
{
    private static WebApplication BuildAppWithController()
    {
        var builder = WebApplication.CreateBuilder();

        // MVC seeds its application parts from the entry assembly. Under the test host that
        // is the runner, not this assembly, so the probe controller has to be added by hand.
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(AuthRouteProbeController).Assembly);

        var app = builder.Build();
        app.MapControllers();
        return app;
    }

    /// <summary>
    /// The source the fix relies on: the route builder's own collection, which
    /// <c>MapControllers</c> writes into, is populated immediately — no host start needed.
    /// </summary>
    [Fact]
    public void RouteBuilderDataSources_SeeControllerEndpoints_BeforeRun()
    {
        var app = BuildAppWithController();

        var dataSources = ((IEndpointRouteBuilder)app).DataSources;
        Assert.NotEmpty(dataSources);

        var patterns = new CompositeEndpointDataSource(dataSources)
            .Endpoints
            .OfType<RouteEndpoint>()
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        Assert.Contains(patterns, p =>
            p is not null && p.Contains("auth/login", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The source that caused the false positive. Pins current framework behaviour: if a
    /// future ASP.NET Core merges the route builder's data sources into DI earlier, this
    /// test fails — at which point it can be deleted, not worked around.
    /// </summary>
    [Fact]
    public void EndpointDataSourceFromDi_IsEmpty_BeforeRun()
    {
        var app = BuildAppWithController();

        var fromDi = app.Services.GetService<EndpointDataSource>();

        Assert.True(
            fromDi is null || fromDi.Endpoints.Count == 0,
            "EndpointDataSource resolved from DI before app.Run() is expected to be empty. " +
            "If it now reports endpoints, LogStartupDiagnostics no longer needs to read " +
            "IEndpointRouteBuilder.DataSources and this guard can be removed.");
    }
}

/// <summary>
/// Stand-in for <c>AuthController</c>: same route shape, no dependencies. Referencing the
/// real controller would pull the whole Hub API — and its startup migration — into the test
/// project. What is under test here is endpoint enumeration, not the controller itself.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthRouteProbeController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login() => Ok();
}
