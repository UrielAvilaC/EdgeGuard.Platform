using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Api;

public static class NodeApiExtensions
{
    /// <summary>
    /// Registers Node API controllers in the service collection.
    /// </summary>
    public static IServiceCollection AddNodeApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(NodeApiExtensions).Assembly);

        return services;
    }

    /// <summary>
    /// Maps Node API controller endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapNodeApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();

        return endpoints;
    }
}
