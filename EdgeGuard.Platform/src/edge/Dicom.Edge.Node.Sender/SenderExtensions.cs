using Dicom.Edge.Abstractions.Configuration;
using Dicom.Edge.Abstractions.Monitoring;
using Dicom.Edge.Node.Sender.Anonymization;
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

        // AE unification: the outbound SCU Calling AE is DERIVED from the single
        // source of truth (DicomServer:AeTitle), never configured independently.
        // PostConfigure re-runs on IOptionsMonitor reload, so Hub config pushes that
        // change the canonical AE propagate here without a restart.
        services.PostConfigure<PacsSenderOptions>(o =>
            o.LocalAeTitle = configuration[NodeAeTitle.ConfigPath] ?? NodeAeTitle.Default);

        // P0-2: DICOM PS3.15 Basic Confidentiality anonymizer.
        services.AddSingleton<IDicomAnonymizer, BasicDicomAnonymizer>();
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
