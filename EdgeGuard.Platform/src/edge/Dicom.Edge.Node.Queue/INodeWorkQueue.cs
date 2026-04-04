using Dicom.Edge.Common.Results;

namespace Dicom.Edge.Node.Queue;

/// <summary>
/// High-level queue operations for the node processing pipeline.
/// Wraps <see cref="Dicom.Edge.Abstractions.Queue.IEdgeQueue{T}"/> with domain-specific semantics.
/// </summary>
public interface INodeWorkQueue
{
    Task<Result> EnqueueAsync(NodeWorkItem item, CancellationToken ct = default);
    Task<Result<NodeWorkItem?>> DequeueAsync(CancellationToken ct = default);
    Task<Result<int>> GetPendingCountAsync(CancellationToken ct = default);
    Task<Result<int>> GetFailedCountAsync(CancellationToken ct = default);
}
