using Dicom.Edge.Node.Diagnostics.Configuration;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;

namespace Dicom.Edge.Node.Diagnostics.Enrichers;

/// <summary>
/// Enriches every log event with Edge Node identity properties:
/// NodeId, Application, Component and Environment.
/// </summary>
public sealed class NodeEnricher : ILogEventEnricher
{
    private readonly LogEventProperty _nodeId;
    private readonly LogEventProperty _application;
    private readonly LogEventProperty _component;
    private readonly LogEventProperty _environment;

    public NodeEnricher(IOptions<DiagnosticsOptions> options)
    {
        var config = options.Value;

        _nodeId = new LogEventProperty(LoggingConstants.NodeId, new ScalarValue(config.NodeId));
        _application = new LogEventProperty(LoggingConstants.Application, new ScalarValue(config.Application));
        _component = new LogEventProperty(LoggingConstants.Component, new ScalarValue(config.Component));
        _environment = new LogEventProperty(LoggingConstants.Environment, new ScalarValue(config.Environment));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(_nodeId);
        logEvent.AddPropertyIfAbsent(_application);
        logEvent.AddPropertyIfAbsent(_component);
        logEvent.AddPropertyIfAbsent(_environment);
    }
}
