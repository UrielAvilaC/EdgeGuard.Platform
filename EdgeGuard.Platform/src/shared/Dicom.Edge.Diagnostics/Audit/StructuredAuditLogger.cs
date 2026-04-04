using Dicom.Edge.Abstractions.Audit;
using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Correlation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Diagnostics.Audit;

/// <summary>
/// Structured-logging-backed implementation of <see cref="IAuditLogger"/>.
/// Writes audit events as structured log entries with a dedicated "Audit" source context,
/// enabling filtering, routing to separate sinks, and compliance queries.
/// Every audit event automatically includes CorrelationId and InstanceId for traceability.
/// </summary>
public sealed class StructuredAuditLogger : IAuditLogger
{
    private readonly ILogger _logger;
    private readonly string _instanceId;

    public StructuredAuditLogger(ILoggerFactory loggerFactory, IOptions<DiagnosticsOptions> options)
    {
        _logger = loggerFactory.CreateLogger("Audit");
        _instanceId = options.Value.InstanceId;
    }

    /// <inheritdoc/>
    public Task LogEventAsync(
        string eventType,
        string action,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        using var scope = BeginAuditScope();
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
        using var scope = BeginAuditScope();
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

        using var scope = BeginAuditScope();
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
        using var scope = BeginAuditScope();
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
        using var scope = BeginAuditScope();
        _logger.LogWarning(
            "AuditConfigChange {AuditUserId} {AuditConfigType} {AuditChanges}",
            userId, configType, changes);

        return Task.CompletedTask;
    }

    private IDisposable? BeginAuditScope()
    {
        return _logger.BeginScope(new Dictionary<string, object?>
        {
            ["AuditCategory"] = "Audit",
            [DiagnosticsConstants.InstanceId] = _instanceId,
            [DiagnosticsConstants.CorrelationId] = CorrelationScope.CurrentCorrelationId
        });
    }
}
