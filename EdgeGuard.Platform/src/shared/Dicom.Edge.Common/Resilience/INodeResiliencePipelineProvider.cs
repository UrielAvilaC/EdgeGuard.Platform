using Polly;

namespace Dicom.Edge.Common.Resilience;

/// <summary>
/// P0-10: Provides ONE Polly resilience pipeline (retry + circuit breaker + timeout)
/// PER NODE. Each unique nodeId gets its own isolated pipeline instance so a single
/// unreachable node cannot trip the breaker for all other nodes.
/// </summary>
public interface INodeResiliencePipelineProvider
{
    /// <summary>
    /// Returns the cached resilience pipeline for the given node, creating it on
    /// first use. Thread-safe — concurrent callers with the same nodeId receive
    /// the same pipeline instance.
    /// </summary>
    ResiliencePipeline<HttpResponseMessage> GetForNode(string nodeId);
}
