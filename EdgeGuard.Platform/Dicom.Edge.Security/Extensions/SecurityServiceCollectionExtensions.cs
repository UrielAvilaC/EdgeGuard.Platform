using Dicom.Edge.Security.Authentication;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dicom.Edge.Security.Extensions
{
    public static class SecurityServiceCollectionExtensions
    {
        public static IServiceCollection AddEdgeSecurity(
            this IServiceCollection services,
            string jwtSecret)
        {
            services.AddSingleton<ITokenService>(
                new JwtTokenService(jwtSecret));

            return services;
        }
    }
}
