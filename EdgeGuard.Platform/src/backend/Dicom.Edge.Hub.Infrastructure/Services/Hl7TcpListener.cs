using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// Implementación del listener TCP para mensajes HL7.
/// Soporta múltiples conexiones concurrentes y procesamiento paralelo.
/// </summary>
public class Hl7TcpListener : IHl7Listener
{
    private readonly ILogger<Hl7TcpListener> _logger;
    private readonly IHl7MessageRepository _repository;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly Hl7ListenerOptions _options;
    private readonly Channel<Hl7Message> _messageChannel;
    private readonly SemaphoreSlim _connectionSemaphore;
    
    private TcpListener? _listener;
    private int _activeConnections;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public int Port => _options.Port;
    public int ActiveConnections => _activeConnections;

    public Hl7TcpListener(
        ILogger<Hl7TcpListener> logger,
        IHl7MessageRepository repository,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<Hl7ListenerOptions> options)
    {
        _logger = logger;
        _repository = repository;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;

        _messageChannel = Channel.CreateBounded<Hl7Message>(
            new BoundedChannelOptions(_options.MaxQueuedMessages)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        _connectionSemaphore = new SemaphoreSlim(_options.MaxConcurrentConnections);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("HL7 Listener is disabled in configuration");
            return;
        }

        if (_isRunning)
        {
            _logger.LogWarning("HL7 Listener is already running");
            return;
        }

        _logger.LogInformation(
            "Starting HL7 Listener on port {Port} with {Workers} workers",
            _options.Port,
            _options.ProcessingWorkers);

        // Iniciar workers de procesamiento
        var processingTasks = Enumerable.Range(0, _options.ProcessingWorkers)
            .Select(i => ProcessMessagesAsync(i, cancellationToken))
            .ToArray();

        // Iniciar listener TCP
        _listener = new TcpListener(IPAddress.Any, _options.Port);
        _listener.Start();
        _isRunning = true;

        _logger.LogInformation("HL7 Listener started successfully on port {Port}", _options.Port);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("HL7 Listener stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HL7 Listener");
            throw;
        }
        finally
        {
            await StopAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping HL7 Listener...");

        _isRunning = false;
        _listener?.Stop();
        _messageChannel.Writer.Complete();

        _logger.LogInformation("HL7 Listener stopped");
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        Interlocked.Increment(ref _activeConnections);

        try
        {
            using (client)
            {
                var endpoint = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
                _logger.LogInformation(
                    "Client connected: {Endpoint}. Active connections: {ActiveConnections}",
                    endpoint,
                    _activeConnections);

                var stream = client.GetStream();
                var buffer = new byte[_options.BufferSize];

                using var cts = new CancellationTokenSource(_options.ConnectionTimeoutMs);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                while (!linkedCts.Token.IsCancellationRequested && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, linkedCts.Token);

                    if (bytesRead == 0) break;

                    var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = Hl7Message.Create(content, endpoint);

                    // Persistir mensaje
                    await _repository.AddAsync(message, linkedCts.Token);

                    _logger.LogInformation(
                        "Message {MessageId} received from {Endpoint}, Type: {MessageType}",
                        message.Id,
                        endpoint,
                        message.MessageType);

                    // Encolar para procesamiento
                    await _messageChannel.Writer.WriteAsync(message, linkedCts.Token);

                    // Enviar ACK
                    var ack = BuildHl7Ack(message);
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(ack), linkedCts.Token);
                }

                _logger.LogInformation("Client disconnected: {Endpoint}", endpoint);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Connection timeout or cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling client connection");
        }
        finally
        {
            Interlocked.Decrement(ref _activeConnections);
            _connectionSemaphore.Release();
        }
    }

    private async Task ProcessMessagesAsync(int workerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing worker {WorkerId} started", workerId);

        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                _logger.LogDebug("Worker {WorkerId} processing message {MessageId}", workerId, message.Id);

                using var scope = _serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IHl7MessageProcessor>();
                await processor.ProcessAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} failed to process message {MessageId}", workerId, message.Id);
            }
        }

        _logger.LogInformation("Processing worker {WorkerId} stopped", workerId);
    }

    private string BuildHl7Ack(Hl7Message originalMessage)
    {
        // HL7 requiere un timestamp con zona horaria
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // ID único para el ACK
        var ackMessageControlId = Guid.NewGuid().ToString("N")[..10].ToUpper();

        // Extraer el Message Control ID del mensaje original
        var originalMessageControlId = ExtractMessageControlId(originalMessage.Content) ?? ackMessageControlId;

        // Construir ACK según el estándar HL7 v2.x
        // Formato: <VT>MSH|...<CR>MSA|...<FS><CR>
        var ackSegments = 
            $"MSH|^~\\&|EdgeGuardHub|EdgeGuard|{originalMessage.SendingApplication}|{originalMessage.SendingFacility}|{timestamp}||ACK|{ackMessageControlId}|P|2.5\r" +
            $"MSA|AA|{originalMessageControlId}\r";

        // Envolver con delimitadores HL7
        return $"\x0B{ackSegments}\x1C\r";
    }

    private string? ExtractMessageControlId(string hl7Message)
    {
        try
        {
            // Limpiar caracteres de control
            var cleanMessage = hl7Message.Replace("\x0B", "").Replace("\x1C", "").Replace("\r", "").Replace("\n", "");

            // El MSH tiene la estructura: MSH|^~\&|campo3|campo4|...|campo9=MessageControlId
            var mshSegment = cleanMessage.Split('|');
            if (mshSegment.Length > 9)
            {
                return mshSegment[9]; // Message Control ID está en el campo 10 (índice 9)
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract Message Control ID from HL7 message");
        }

        return null;
    }
}
