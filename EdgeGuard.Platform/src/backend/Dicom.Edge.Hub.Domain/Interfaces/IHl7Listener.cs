namespace Dicom.Edge.Hub.Domain.Interfaces;

/// <summary>
/// Interfaz para el servicio que escucha conexiones HL7.
/// </summary>
public interface IHl7Listener
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
    int Port { get; }
    int ActiveConnections { get; }
}
