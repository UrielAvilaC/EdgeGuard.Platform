using Dicom.Edge.Diagnostics.Correlation;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Diagnostics.Logging;

/// <summary>
/// <see cref="ILogger"/> decorator that re-enters an <see cref="AssociationLogContext"/> around
/// every log call, so events emitted outside our own async flow are still attributed to the
/// association.
/// </summary>
/// <remarks>
/// This is what captures <b>fo-dicom's own</b> logging (PDU/DIMSE traffic, timeouts): those
/// events are written from the connection read loop, which never flows the ambient context set
/// inside the SCP callbacks. Assign an instance to <c>DicomService.Logger</c> in the SCP
/// constructor — fo-dicom creates one service instance per association, so the mapping
/// instance ↔ association is exact.
/// </remarks>
public sealed class AssociationScopedLogger(ILogger inner, AssociationLogContext context) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        using var scope = DicomAssociationContext.Enter(context);
        inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
