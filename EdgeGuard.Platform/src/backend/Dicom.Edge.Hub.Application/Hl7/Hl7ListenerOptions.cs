namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Opciones de configuración para el listener HL7.
/// </summary>
public class Hl7ListenerOptions
{
    public const string SectionName = "Hl7Listener";

    /// <summary>
    /// Puerto TCP para el listener HL7.
    /// </summary>
    public int Port { get; set; } = 8001;

    /// <summary>
    /// Número máximo de conexiones concurrentes.
    /// </summary>
    public int MaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// Número máximo de mensajes en cola.
    /// </summary>
    public int MaxQueuedMessages { get; set; } = 1000;

    /// <summary>
    /// Número de workers para procesar mensajes en paralelo.
    /// </summary>
    public int ProcessingWorkers { get; set; } = 4;

    /// <summary>
    /// Timeout de conexión en milisegundos.
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 300000; // 5 minutos

    /// <summary>
    /// Tamaño del buffer de lectura.
    /// </summary>
    public int BufferSize { get; set; } = 8192;

    /// <summary>
    /// Habilitar el listener al iniciar la aplicación.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// P0-5: When true, validates HL7 messages BEFORE sending ACK. Invalid messages
    /// receive a NACK (AE / Application Error) and are persisted with
    /// <c>DispatchStatus = ValidationFailed</c>. When false (default), all syntactically
    /// MLLP-framed messages receive AA — the previous behaviour. Recommended: enable
    /// in production after a one-week monitoring window of NACK rates per HIS.
    /// </summary>
    public bool ValidateBeforeAck { get; set; } = false;
}
