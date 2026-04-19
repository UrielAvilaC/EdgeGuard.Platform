using Dicom.Edge.Security.Authentication;
using Dicom.Edge.Security.Authorization;
using Dicom.Edge.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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
            services.AddSingleton<Dicom.Edge.Security.Authorization.IAuthorizationService, AuthorizationService>();

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
            services.AddSingleton<Dicom.Edge.Security.Authorization.IAuthorizationService, AuthorizationService>();

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
            services.AddSingleton<Dicom.Edge.Security.Authorization.IAuthorizationService, AuthorizationService>();

            return services;
        }

        /// <summary>
        /// Adds JWT Bearer authentication and permission-based authorization to the ASP.NET pipeline.
        /// Call this in Hub.Api to wire <c>UseAuthentication()</c> + <c>UseAuthorization()</c>.
        /// </summary>
        public static IServiceCollection AddEdgeAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection("Jwt");
            var secretKey = jwtSection["SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
            var issuer = jwtSection["Issuer"] ?? "EdgeGuard.Platform";
            var audience = jwtSection["Audience"] ?? "EdgeGuard.Clients";
            var key = Encoding.UTF8.GetBytes(secretKey);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !IsDevEnvironment(configuration);
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.Scheme, _ => { });

            // Permission-based authorization
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            // Password hasher
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            return services;
        }

        private static bool IsDevEnvironment(IConfiguration configuration)
        {
            var env = configuration["ASPNETCORE_ENVIRONMENT"]
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
        }
    }
}
