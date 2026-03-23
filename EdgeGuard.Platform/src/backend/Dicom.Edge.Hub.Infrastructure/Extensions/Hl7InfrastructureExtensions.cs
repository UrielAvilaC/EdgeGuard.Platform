using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Infrastructure.HostedServices;
using Dicom.Edge.Hub.Infrastructure.Repositories;
using Dicom.Edge.Hub.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Infrastructure.Extensions;

/// <summary>
/// Extension methods para configurar la infraestructura de HL7.
/// </summary>
public static class Hl7InfrastructureExtensions
{
    /// <summary>
    /// Agrega servicios de infraestructura para HL7.
    /// </summary>
    public static IServiceCollection AddHl7Infrastructure(this IServiceCollection services)
    {
        // Registrar repositorio (en producción cambiar a persistencia real)
        services.AddSingleton<IHl7MessageRepository, InMemoryHl7MessageRepository>();

        // Registrar listener TCP
        services.AddSingleton<IHl7Listener, Hl7TcpListener>();

        // Registrar el hosted service que ejecutará el listener
        services.AddHostedService<Hl7ListenerHostedService>();

        return services;
    }
}
