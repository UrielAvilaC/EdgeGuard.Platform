using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Hub.Diagnostics.Constants;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Diagnostics.Scopes;

/// <summary>
/// Creates structured logging scopes pre-enriched with HL7 pipeline context.
/// Ensures all log events within the scope carry message ID, type, and routing metadata
/// without requiring manual property injection.
/// </summary>
public static class Hl7LogScope
{
    /// <summary>
    /// Begins a logging scope for an HL7 message processing operation.
    /// </summary>
    public static IDisposable? BeginHl7MessageScope(
        this ILogger logger,
        Guid messageId,
        string? messageType = null,
        string? triggerEvent = null,
        string? operationName = null)
    {
        var state = new Dictionary<string, object?>
        {
            [HubDiagnosticsConstants.Hl7MessageId] = messageId,
        };

        if (messageType is not null)
            state[HubDiagnosticsConstants.Hl7MessageType] = messageType;

        if (triggerEvent is not null)
            state[HubDiagnosticsConstants.Hl7TriggerEvent] = triggerEvent;

        if (operationName is not null)
            state[DiagnosticsConstants.OperationName] = operationName;

        return logger.BeginScope(state);
    }

    /// <summary>
    /// Begins a logging scope for an HL7 message dispatch operation.
    /// Combines message identity with dispatch routing metadata.
    /// </summary>
    public static IDisposable? BeginDispatchScope(
        this ILogger logger,
        Guid messageId,
        string targetNodeId,
        int attempt)
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            [HubDiagnosticsConstants.Hl7MessageId] = messageId,
            [HubDiagnosticsConstants.DispatchTargetNodeId] = targetNodeId,
            [HubDiagnosticsConstants.DispatchAttempt] = attempt,
            [DiagnosticsConstants.OperationName] = "DispatchHl7Message",
        });
    }

    /// <summary>
    /// Begins a logging scope for HL7 TCP connection handling.
    /// </summary>
    public static IDisposable? BeginHl7ConnectionScope(
        this ILogger logger,
        string remoteEndpoint,
        string? sendingFacility = null,
        string? sendingApplication = null)
    {
        var state = new Dictionary<string, object?>
        {
            [DiagnosticsConstants.OperationName] = "Hl7TcpConnection",
            ["RemoteEndpoint"] = remoteEndpoint,
        };

        if (sendingFacility is not null)
            state[HubDiagnosticsConstants.Hl7SendingFacility] = sendingFacility;

        if (sendingApplication is not null)
            state[HubDiagnosticsConstants.Hl7SendingApplication] = sendingApplication;

        return logger.BeginScope(state);
    }
}
