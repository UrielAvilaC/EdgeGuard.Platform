using Dicom.Edge.Hub.Application.Hl7;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Application.Extensions;

/// <summary>
/// Extension methods para configurar servicios HL7.
/// </summary>
public static class Hl7ServiceCollectionExtensions
{
    /// <summary>
    /// Agrega servicios de la capa de aplicación para HL7.
    /// </summary>
    public static IServiceCollection AddHl7Application(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar opciones
        services.Configure<Hl7ListenerOptions>(
            configuration.GetSection(Hl7ListenerOptions.SectionName));

        // Registrar procesador de mensajes
        services.AddScoped<IHl7MessageProcessor, Hl7MessageProcessor>();

        return services;
    }
}
