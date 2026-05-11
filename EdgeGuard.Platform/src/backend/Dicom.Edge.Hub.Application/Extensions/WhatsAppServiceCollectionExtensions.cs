using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Application.WhatsApp;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Hub.Application.Extensions;

public static class WhatsAppServiceCollectionExtensions
{
    public static IServiceCollection AddWhatsAppServices(this IServiceCollection services)
    {
        services.AddScoped<IWhatsAppTemplateService, WhatsAppTemplateService>();
        services.AddScoped<IWhatsAppAutoSendRuleService, WhatsAppAutoSendRuleService>();
        services.AddScoped<IWhatsAppNotificationService, WhatsAppNotificationService>();
        services.AddScoped<IHl7PatientSyncService, Hl7PatientSyncService>();

        return services;
    }
}
