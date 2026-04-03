using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Dicom.Edge.Abstractions.Metrics;

namespace Dicom.Edge.Node.Diagnostics.Observability;

/// <summary>
/// OpenTelemetry-backed implementation of <see cref="IMetricsCollector"/>.
/// Uses <see cref="System.Diagnostics.Metrics.Meter"/> for standard OTEL instrumentation.
/// All instruments are created once and reused (thread-safe).
/// </summary>
public sealed class OpenTelemetryMetricsCollector : IMetricsCollector, IDisposable
{
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _studiesReceivedCounter;
    private readonly Counter<long> _transferStartedCounter;
    private readonly Counter<long> _transferCompletedCounter;
    private readonly Counter<long> _transferFailedCounter;
    private readonly Counter<long> _errorsCounter;
    private readonly Counter<long> _associationsCounter;

    // Histograms
    private readonly Histogram<double> _transferDurationHistogram;
    private readonly Histogram<long> _studySizeHistogram;

    // Gauges (reported via ObservableGauge callbacks)
    private long _currentQueueDepth;
    private long _availableStorageMb;
    private long _totalStorageMb;
    private double _cpuPercent;
    private double _memoryMb;

    // Caches for dynamically created instruments — prevents duplicate creation
    private readonly ConcurrentDictionary<string, Histogram<double>> _histogramCache = new();
    private readonly ConcurrentDictionary<string, Counter<long>> _counterCache = new();

    public OpenTelemetryMetricsCollector()
    {
        _meter = new Meter(LoggingConstants.MeterName, "1.0.0");

        _studiesReceivedCounter = _meter.CreateCounter<long>(
            "edge.studies.received",
            unit: "{study}",
            description: "Total number of studies received from modalities");

        _transferStartedCounter = _meter.CreateCounter<long>(
            "edge.transfers.started",
            unit: "{transfer}",
            description: "Total number of transfer operations started");

        _transferCompletedCounter = _meter.CreateCounter<long>(
            "edge.transfers.completed",
            unit: "{transfer}",
            description: "Total number of transfer operations completed successfully");

        _transferFailedCounter = _meter.CreateCounter<long>(
            "edge.transfers.failed",
            unit: "{transfer}",
            description: "Total number of transfer operations that failed");

        _errorsCounter = _meter.CreateCounter<long>(
            "edge.errors",
            unit: "{error}",
            description: "Total number of errors recorded");

        _associationsCounter = _meter.CreateCounter<long>(
            "edge.associations",
            unit: "{association}",
            description: "Total number of DICOM association events");

        _transferDurationHistogram = _meter.CreateHistogram<double>(
            "edge.transfer.duration",
            unit: "s",
            description: "Duration of transfer operations in seconds");

        _studySizeHistogram = _meter.CreateHistogram<long>(
            "edge.study.size",
            unit: "By",
            description: "Size of received studies in bytes");

        _meter.CreateObservableGauge(
            "edge.queue.depth",
            () => _currentQueueDepth,
            unit: "{item}",
            description: "Current number of items pending in the queue");

        _meter.CreateObservableGauge(
            "edge.storage.available",
            () => _availableStorageMb,
            unit: "MiBy",
            description: "Available storage space in MB");

        _meter.CreateObservableGauge(
            "edge.storage.total",
            () => _totalStorageMb,
            unit: "MiBy",
            description: "Total storage capacity in MB");

        _meter.CreateObservableGauge(
            "edge.system.cpu",
            () => _cpuPercent,
            unit: "%",
            description: "Current CPU usage percentage");

        _meter.CreateObservableGauge(
            "edge.system.memory",
            () => _memoryMb,
            unit: "MiBy",
            description: "Current memory usage in MB");
    }

    /// <inheritdoc/>
    public void RecordStudyReceived(string studyInstanceUid, long sizeBytes, int instanceCount)
    {
        // Note: study_uid is NOT included as a tag to avoid high-cardinality explosion
        // in metrics backends. Use structured logging for per-study traceability instead.
        _studiesReceivedCounter.Add(1,
            new KeyValuePair<string, object?>("instance_count", instanceCount));

        _studySizeHistogram.Record(sizeBytes);
    }

    /// <inheritdoc/>
    public void RecordTransferStarted(string studyInstanceUid, string destination)
    {
        _transferStartedCounter.Add(1,
            new KeyValuePair<string, object?>("destination", destination));
    }

    /// <inheritdoc/>
    public void RecordTransferCompleted(string studyInstanceUid, TimeSpan duration, bool success)
    {
        if (success)
        {
            _transferCompletedCounter.Add(1);
        }
        else
        {
            _transferFailedCounter.Add(1);
        }

        _transferDurationHistogram.Record(duration.TotalSeconds,
            new KeyValuePair<string, object?>("success", success));
    }

    /// <inheritdoc/>
    public void RecordError(string errorType, string message, int severity = 2)
    {
        _errorsCounter.Add(1,
            new KeyValuePair<string, object?>("error_type", errorType),
            new KeyValuePair<string, object?>("severity", severity));
    }

    /// <inheritdoc/>
    public void RecordAssociation(string callingAeTitle, string action, bool success)
    {
        _associationsCounter.Add(1,
            new KeyValuePair<string, object?>("calling_ae", callingAeTitle),
            new KeyValuePair<string, object?>("action", action),
            new KeyValuePair<string, object?>("success", success));
    }

    /// <inheritdoc/>
    public void RecordQueueDepth(string queueName, int depth)
    {
        Interlocked.Exchange(ref _currentQueueDepth, depth);
    }

    /// <inheritdoc/>
    public void RecordStorageUsage(long availableMb, long totalMb)
    {
        Interlocked.Exchange(ref _availableStorageMb, availableMb);
        Interlocked.Exchange(ref _totalStorageMb, totalMb);
    }

    /// <inheritdoc/>
    public void RecordResourceUsage(double cpuPercent, double memoryMb)
    {
        Volatile.Write(ref _cpuPercent, cpuPercent);
        Volatile.Write(ref _memoryMb, memoryMb);
    }

    /// <inheritdoc/>
    public void RecordMetric(string name, double value, Dictionary<string, string>? tags = null)
    {
        var histogram = _histogramCache.GetOrAdd(name, n => _meter.CreateHistogram<double>(n));
        if (tags is { Count: > 0 })
        {
            var tagArray = tags
                .Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value))
                .ToArray();
            histogram.Record(value, tagArray);
        }
        else
        {
            histogram.Record(value);
        }
    }

    /// <inheritdoc/>
    public void IncrementCounter(string name, int increment = 1, Dictionary<string, string>? tags = null)
    {
        var counter = _counterCache.GetOrAdd(name, n => _meter.CreateCounter<long>(n));
        if (tags is { Count: > 0 })
        {
            var tagArray = tags
                .Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value))
                .ToArray();
            counter.Add(increment, tagArray);
        }
        else
        {
            counter.Add(increment);
        }
    }

    /// <inheritdoc/>
    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // OTEL SDK flushes automatically via its pipeline;
        // this is a no-op unless a manual flush strategy is needed.
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
