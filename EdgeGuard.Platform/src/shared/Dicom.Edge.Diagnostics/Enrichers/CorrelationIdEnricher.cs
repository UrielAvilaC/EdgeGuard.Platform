using System.Diagnostics;
using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Correlation;
using Serilog.Core;
using Serilog.Events;

namespace Dicom.Edge.Diagnostics.Enrichers;

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
                propertyFactory.CreateProperty(DiagnosticsConstants.CorrelationId, correlationId));
        }
    }
}
