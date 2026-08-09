using Dicom.Edge.Diagnostics.Constants;
using Dicom.Edge.Diagnostics.Correlation;
using Serilog.Core;
using Serilog.Events;

namespace Dicom.Edge.Diagnostics.Enrichers;

/// <summary>
/// Enriches log events with the ambient DICOM association context
/// (<see cref="DicomAssociationContext"/>) so they can be routed to a per-association file
/// and correlated with the global log.
/// </summary>
public sealed class AssociationEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = DicomAssociationContext.Current;
        if (context is null)
            return;

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(DiagnosticsConstants.AssociationId, context.AssociationId));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(DiagnosticsConstants.CallingAeTitle, context.CallingAe));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(DiagnosticsConstants.CalledAeTitle, context.CalledAe));
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(DiagnosticsConstants.RemoteHost, context.RemoteHost));
    }
}
