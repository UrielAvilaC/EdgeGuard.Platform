using Dicom.Edge.Diagnostics.Configuration;
using Dicom.Edge.Diagnostics.Constants;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;

namespace Dicom.Edge.Diagnostics.Enrichers;

/// <summary>
/// Enriches every log event with platform instance identity properties:
/// InstanceId, Application, Component and Environment.
/// Used by both Hub and Edge Node.
/// </summary>
public sealed class InstanceEnricher : ILogEventEnricher
{
    private readonly LogEventProperty _instanceId;
    private readonly LogEventProperty _application;
    private readonly LogEventProperty _component;
    private readonly LogEventProperty _environment;

    public InstanceEnricher(IOptions<DiagnosticsOptions> options)
    {
        var config = options.Value;

        _instanceId = new LogEventProperty(DiagnosticsConstants.InstanceId, new ScalarValue(config.InstanceId));
        _application = new LogEventProperty(DiagnosticsConstants.Application, new ScalarValue(config.Application));
        _component = new LogEventProperty(DiagnosticsConstants.Component, new ScalarValue(config.Component));
        _environment = new LogEventProperty(DiagnosticsConstants.Environment, new ScalarValue(config.Environment));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(_instanceId);
        logEvent.AddPropertyIfAbsent(_application);
        logEvent.AddPropertyIfAbsent(_component);
        logEvent.AddPropertyIfAbsent(_environment);
    }
}
