using Dicom.Edge.Hub.Domain.Aggregates.Identity;
using Dicom.Edge.Hub.Persistence.Context;
using Dicom.Edge.Security.Authorization;
using Dicom.Edge.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Persistence.Seed;

/// <summary>
/// Idempotent seeder for the default super-administrator user.
/// <para>
/// Credentials are resolved <b>exclusively</b> from environment variables:
/// <list type="bullet">
///   <item><c>EDGEGUARD_ADMIN_USERNAME</c> — optional, defaults to "admin"</item>
///   <item><c>EDGEGUARD_ADMIN_PASSWORD</c> — <b>required</b>, seed is skipped if absent</item>
/// </list>
/// No configuration files (appsettings, User Secrets) are read.
/// This ensures credentials never end up in source control or serialized config.
/// </para>
/// </summary>
public static class AdminUserSeed
{
    private const string EnvUsername = "EDGEGUARD_ADMIN_USERNAME";
    private const string EnvPassword = "EDGEGUARD_ADMIN_PASSWORD";
    private const string DefaultUsername = "admin";

    /// <summary>
    /// Seeds the super-administrator if no admin user exists.
    /// Reads credentials only from environment variables — never from config files.
    /// </summary>
    public static async Task SeedAsync(
        HubDbContext ctx,
        IPasswordHasher passwordHasher,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        var username = Environment.GetEnvironmentVariable(EnvUsername)?.Trim().ToLowerInvariant()
            ?? DefaultUsername;

        var exists = await ctx.Users
            .AnyAsync(u => u.Username == username, ct);

        if (exists)
        {
            logger?.LogDebug("Admin user '{Username}' already exists — skipping seed", username);
            return;
        }

        var password = Environment.GetEnvironmentVariable(EnvPassword);

        if (string.IsNullOrWhiteSpace(password))
        {
            logger?.LogWarning(
                "Admin seed skipped: environment variable '{EnvVar}' is not set. " +
                "Set it to create the default super-administrator on first run.",
                EnvPassword);
            return;
        }

        var policyError = ValidatePasswordPolicy(password);
        if (policyError is not null)
        {
            logger?.LogWarning("Admin seed skipped: {PolicyError}", policyError);
            return;
        }

        var hash = passwordHasher.HashPassword(password);
        var admin = User.Create(username, hash, "System Administrator", createdBy: "system-seed");

        admin.AssignRole(Role.Admin, "system-seed");

        await ctx.Users.AddAsync(admin, ct);
        await ctx.SaveChangesAsync(ct);

        logger?.LogInformation(
            "Default super-administrator created (username: {Username}). " +
            "Change the password after first login.",
            username);
    }

    private static string? ValidatePasswordPolicy(string password)
    {
        if (password.Length < 8)
            return "Password must be at least 8 characters.";
        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";
        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter.";
        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return "Password must contain at least one special character.";
        return null;
    }
}
