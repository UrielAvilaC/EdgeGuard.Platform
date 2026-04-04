using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node.Router;

public static class RouterExtensions
{
    public static IServiceCollection AddNodeRouter(this IServiceCollection services)
    {
        services.AddSingleton<RuleBasedStudyRouter>();
        services.AddSingleton<IStudyRouter>(sp => sp.GetRequiredService<RuleBasedStudyRouter>());

        return services;
    }
}
