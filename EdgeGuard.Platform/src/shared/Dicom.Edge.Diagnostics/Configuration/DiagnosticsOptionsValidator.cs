using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.Configuration;

/// <summary>
/// Validates <see cref="DiagnosticsOptions"/> at startup.
/// Prevents the application from running with invalid diagnostics configuration
/// (fail-fast principle).
/// </summary>
public sealed class DiagnosticsOptionsValidator : IValidateOptions<DiagnosticsOptions>
{
    public ValidateOptionsResult Validate(string? name, DiagnosticsOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.InstanceId))
            failures.Add($"{nameof(options.InstanceId)} is required.");

        if (string.IsNullOrWhiteSpace(options.Application))
            failures.Add($"{nameof(options.Application)} is required.");

        if (options.File.MaxFileSizeMb <= 0)
            failures.Add($"{nameof(options.File)}.{nameof(options.File.MaxFileSizeMb)} must be greater than 0.");

        if (options.File.RetainDays <= 0)
            failures.Add($"{nameof(options.File)}.{nameof(options.File.RetainDays)} must be greater than 0.");

        if (options.Seq.Enabled && string.IsNullOrWhiteSpace(options.Seq.Url))
            failures.Add("Seq is enabled but Url is not configured.");

        if (options.HttpSink.Enabled && string.IsNullOrWhiteSpace(options.HttpSink.Url))
            failures.Add("HTTP sink is enabled but Url is not configured.");

        if (options.OpenTelemetry.Enabled && string.IsNullOrWhiteSpace(options.OpenTelemetry.ServiceName))
            failures.Add("OpenTelemetry is enabled but ServiceName is not configured.");

        if (options.HealthChecks.StorageMinAvailableMb <= 0)
            failures.Add($"{nameof(options.HealthChecks)}.{nameof(options.HealthChecks.StorageMinAvailableMb)} must be greater than 0.");

        if (options.Redaction.Enabled && string.IsNullOrWhiteSpace(options.Redaction.ReplacementToken))
            failures.Add("PHI Redaction is enabled but ReplacementToken is empty.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
