using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Abstractions.Engine
{
    public interface IEdgeModule
    {
        void Register(IServiceCollection services);
    }
}
