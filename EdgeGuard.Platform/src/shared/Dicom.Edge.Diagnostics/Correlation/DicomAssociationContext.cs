namespace Dicom.Edge.Diagnostics.Correlation;

/// <summary>
/// Ambient context carrying the current <see cref="AssociationLogContext"/> across async
/// boundaries, so that everything executed underneath a DICOM SCP callback
/// (instance handler, C-FIND handlers, storage, EF Core) is tagged with the association id
/// without threading the id through every method signature.
/// </summary>
/// <remarks>
/// fo-dicom dispatches each DIMSE callback from its own connection read loop, so a scope
/// opened in <c>OnReceiveAssociationRequestAsync</c> does <b>not</b> flow into
/// <c>OnCStoreRequestAsync</c>. The rule is therefore: <i>one context per association, re-entered
/// at the start of every callback</i>. Events emitted by fo-dicom itself are covered by
/// <c>AssociationScopedLogger</c> instead.
/// </remarks>
public static class DicomAssociationContext
{
    private static readonly AsyncLocal<AssociationLogContext?> _current = new();

    /// <summary>The association context of the current async flow, if any.</summary>
    public static AssociationLogContext? Current => _current.Value;

    /// <summary>
    /// Enters the supplied association context for the current async flow.
    /// Disposing restores the previous value, so nested calls are safe.
    /// </summary>
    public static IDisposable Enter(AssociationLogContext context) => new Scope(context);

    private sealed class Scope : IDisposable
    {
        private readonly AssociationLogContext? _previous;
        private bool _disposed;

        internal Scope(AssociationLogContext context)
        {
            _previous = _current.Value;
            _current.Value = context;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _current.Value = _previous;
        }
    }
}
