using Dicom.Edge.Abstractions.Monitoring;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Sender;

public static class SenderExtensions
{
    public static IServiceCollection AddNodeSender(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PacsSenderOptions>(
            configuration.GetSection(PacsSenderOptions.SectionName));

        services.AddSingleton<IPacsSender, FoDicomPacsSender>();

        // PACS C-ECHO periodic monitor
        services.Configure<PacsCEchoOptions>(
            configuration.GetSection(PacsCEchoOptions.SectionName));

        services.AddSingleton<PacsCEchoHostedService>();
        services.AddSingleton<IPacsCEchoMonitor>(sp => sp.GetRequiredService<PacsCEchoHostedService>());
        services.AddHostedService(sp => sp.GetRequiredService<PacsCEchoHostedService>());

        return services;
    }
}
