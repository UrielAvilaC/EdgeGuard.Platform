using Dicom.Edge.Abstractions.Engine;
using Dicom.Edge.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dicom.Edge.Abstractions.Events
{
    /// <summary>
    /// Module for registering event bus services in the dependency injection container.
    /// </summary>
    /// <remarks>
    /// This module registers the <see cref="IEventBus"/> service as a singleton.
    /// </remarks>
    public sealed class EventBusModule : IEdgeModule
    {
        /// <inheritdoc />
        public void Register(IServiceCollection services)
        {
            services.TryAddSingleton<IEventBus, InMemoryEventBus>();
        }
    }
}
