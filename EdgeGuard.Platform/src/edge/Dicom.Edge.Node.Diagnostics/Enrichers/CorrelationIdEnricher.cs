using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Dicom.Edge.Node.Diagnostics.Enrichers;

/// <summary>
/// Enriches log events with the current correlation ID from
/// <see cref="Activity.Current"/> or <see cref="CorrelationScope"/>.
/// </summary>
public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = CorrelationScope.CurrentCorrelationId
                            ?? Activity.Current?.TraceId.ToString();

        if (!string.IsNullOrEmpty(correlationId))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(LoggingConstants.CorrelationId, correlationId));
        }
    }
}
