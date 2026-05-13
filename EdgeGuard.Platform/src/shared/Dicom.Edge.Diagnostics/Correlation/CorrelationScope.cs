namespace Dicom.Edge.Diagnostics.Correlation;

/// <summary>
/// Provides an ambient correlation context for propagating correlation IDs
/// across async boundaries, particularly in BackgroundService scenarios
/// where there is no HTTP request pipeline.
/// </summary>
public sealed class CorrelationScope : IDisposable
{
    private static readonly AsyncLocal<string?> _current = new();

    private readonly string? _previousValue;

    private CorrelationScope(string correlationId)
    {
        _previousValue = _current.Value;
        _current.Value = correlationId;
    }

    /// <summary>
    /// Gets the current correlation ID from the ambient scope, if any.
    /// </summary>
    public static string? CurrentCorrelationId => _current.Value;

    /// <summary>
    /// Starts a new correlation scope with the specified ID.
    /// </summary>
    public static CorrelationScope Start(string? correlationId = null)
    {
        return new CorrelationScope(correlationId ?? Guid.NewGuid().ToString("N"));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _current.Value = _previousValue;
    }
}
