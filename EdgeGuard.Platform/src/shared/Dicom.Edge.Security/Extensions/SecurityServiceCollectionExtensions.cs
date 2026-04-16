using Dicom.Edge.Security.Authentication;
using Dicom.Edge.Security.Authorization;
using Dicom.Edge.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Security.Extensions
{
    /// <summary>
    /// Extension methods for configuring EdgeGuard security services.
    /// </summary>
    public static class SecurityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds EdgeGuard security services with JWT authentication.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Configuration section for JWT options.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddEdgeSecurity(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure JWT options from configuration
            services.Configure<JwtTokenServiceOptions>(configuration.GetSection("Jwt"));

            // Register token service
            services.AddSingleton<ITokenService, JwtTokenService>();

            // Register authorization service
            services.AddSingleton<IAuthorizationService, AuthorizationService>();

            // Data Protection + setting encryption
            services.AddDataProtection()
                .SetApplicationName("EdgeGuard.Platform");
            services.AddSingleton<ISettingEncryptionService, DataProtectionSettingEncryptionService>();

            return services;
        }

        /// <summary>
        /// Adds EdgeGuard security services with manual JWT configuration.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configureOptions">Action to configure JWT options.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddEdgeSecurity(
            this IServiceCollection services,
            Action<JwtTokenServiceOptions> configureOptions)
        {
            services.Configure(configureOptions);
            services.AddSingleton<ITokenService, JwtTokenService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();

            return services;
        }

        /// <summary>
        /// Adds EdgeGuard security services with legacy string-based JWT secret (backward compatibility).
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="jwtSecret">JWT secret key (minimum 32 characters).</param>
        /// <returns>Service collection for chaining.</returns>
        [Obsolete("Use AddEdgeSecurity(IConfiguration) or AddEdgeSecurity(Action<JwtTokenServiceOptions>) instead")]
        public static IServiceCollection AddEdgeSecurity(
            this IServiceCollection services,
            string jwtSecret)
        {
            if (string.IsNullOrWhiteSpace(jwtSecret))
                throw new ArgumentException("JWT secret cannot be empty", nameof(jwtSecret));

            if (jwtSecret.Length < 32)
                throw new ArgumentException("JWT secret must be at least 32 characters", nameof(jwtSecret));

            services.Configure<JwtTokenServiceOptions>(options =>
            {
                options.SecretKey = jwtSecret;
                options.Issuer = "EdgeGuard.Platform";
                options.Audience = "EdgeGuard.Clients";
            });

            services.AddSingleton<ITokenService, JwtTokenService>();
            services.AddSingleton<IAuthorizationService, AuthorizationService>();

            return services;
        }
    }
}
