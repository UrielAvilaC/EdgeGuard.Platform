using System.Threading;

namespace Dicom.Edge.Common.Context
{
    /// <summary>
    /// Provides correlation ID for tracking requests across services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Correlation IDs enable distributed tracing by:
    /// <list type="bullet">
    ///   <item><description>Tracking a request across multiple services</description></item>
    ///   <item><description>Correlating logs from different components</description></item>
    ///   <item><description>Debugging end-to-end flows</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICorrelationIdProvider
    {
        /// <summary>
        /// Gets the current correlation ID.
        /// </summary>
        string GetCorrelationId();

        /// <summary>
        /// Sets the correlation ID for the current context.
        /// </summary>
        void SetCorrelationId(string correlationId);

        /// <summary>
        /// Generates a new correlation ID.
        /// </summary>
        string GenerateCorrelationId();
    }

    /// <summary>
    /// Default implementation using AsyncLocal for async context flow.
    /// </summary>
    public class CorrelationIdProvider : ICorrelationIdProvider
    {
        private static readonly AsyncLocal<string?> _correlationId = new();

        public string GetCorrelationId()
        {
            return _correlationId.Value ?? GenerateCorrelationId();
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId.Value = correlationId;
        }

        public string GenerateCorrelationId()
        {
            var id = Guid.NewGuid().ToString("N");
            _correlationId.Value = id;
            return id;
        }
    }
}
