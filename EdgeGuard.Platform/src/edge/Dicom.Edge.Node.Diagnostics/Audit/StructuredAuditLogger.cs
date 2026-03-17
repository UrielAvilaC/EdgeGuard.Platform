using Dicom.Edge.Abstractions.Audit;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Diagnostics.Audit;

/// <summary>
/// Structured-logging-backed implementation of <see cref="IAuditLogger"/>.
/// Writes audit events as structured log entries with a dedicated "Audit" source context,
/// enabling filtering, routing to separate sinks, and compliance queries.
/// </summary>
/// <remarks>
/// Audit events are always logged at <see cref="LogLevel.Information"/> or higher
/// to ensure they are never filtered out by minimum level configuration.
/// The dedicated source context allows routing audit logs to a separate sink
/// (e.g., tamper-evident file or database) via Serilog filter expressions.
/// </remarks>
public sealed class StructuredAuditLogger : IAuditLogger
{
    private readonly ILogger _logger;

    public StructuredAuditLogger(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Audit");
    }

    /// <inheritdoc/>
    public Task LogEventAsync(
        string eventType,
        string action,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AuditEvent {AuditEventType} {AuditAction} {AuditDetails}",
            eventType, action, details);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LogStudyAccessAsync(
        string studyInstanceUid,
        string? userId,
        string action,
        bool isSuccess = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AuditStudyAccess {StudyInstanceUID} {AuditUserId} {AuditAction} {AuditSuccess}",
            studyInstanceUid, userId ?? "system", action, isSuccess);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LogSecurityEventAsync(
        string eventType,
        string? userId,
        string details,
        int severity = 1,
        CancellationToken cancellationToken = default)
    {
        var logLevel = severity switch
        {
            0 => LogLevel.Information,
            1 => LogLevel.Warning,
            2 => LogLevel.Error,
            _ => LogLevel.Critical
        };

        _logger.Log(logLevel,
            "AuditSecurity {AuditEventType} {AuditUserId} {AuditDetails} {AuditSeverity}",
            eventType, userId ?? "system", details, severity);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LogAssociationEventAsync(
        string callingAeTitle,
        string calledAeTitle,
        string action,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AuditAssociation {CallingAeTitle} {CalledAeTitle} {AuditAction} {AuditDetails}",
            callingAeTitle, calledAeTitle, action, details);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task LogConfigurationChangeAsync(
        string userId,
        string configType,
        string changes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "AuditConfigChange {AuditUserId} {AuditConfigType} {AuditChanges}",
            userId, configType, changes);

        return Task.CompletedTask;
    }
}
