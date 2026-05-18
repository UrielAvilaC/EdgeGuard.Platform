using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Dicom.Edge.Hub.Application.Hl7;
using Dicom.Edge.Hub.Application.Hl7.Pipeline;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.Services;

/// <summary>
/// TCP listener for HL7 v2.x messages using MLLP (Minimum Lower Layer Protocol) framing.
///
/// MLLP envelope:
///   Start: 0x0B (VT)
///   End:   0x1C 0x0D (FS + CR)
///
/// A single TCP read may contain partial messages or multiple messages.
/// <see cref="ReadMllpMessagesAsync"/> accumulates bytes across reads and yields
/// one complete HL7 message per iteration.
/// </summary>
public class Hl7TcpListener : IHl7Listener
{
    private readonly ILogger<Hl7TcpListener> _logger;
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
        IServiceScopeFactory serviceScopeFactory,
        IOptions<Hl7ListenerOptions> options)
    {
        _logger = logger;
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
            "Starting HL7 Listener on port {Port} with {Workers} workers (MLLP framing)",
            _options.Port, _options.ProcessingWorkers);

        var processingTasks = Enumerable.Range(0, _options.ProcessingWorkers)
            .Select(i => ProcessMessagesAsync(i, cancellationToken))
            .ToArray();

        _listener = new TcpListener(IPAddress.Any, _options.Port);
        _listener.Start();
        _isRunning = true;

        _logger.LogInformation("HL7 Listener started on port {Port}", _options.Port);

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
            _logger.LogInformation("HL7 Listener stopping…");
        }
        catch (SocketException se) when (
            se.SocketErrorCode == SocketError.OperationAborted ||
            cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("HL7 Listener stopped (socket aborted by host shutdown)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in HL7 Listener");
            throw;
        }
        finally
        {
            await StopAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        _logger.LogInformation("Stopping HL7 Listener…");
        _isRunning = false;
        _listener?.Stop();
        _messageChannel.Writer.Complete();
        _listener?.Dispose();
        _listener = null;
        _logger.LogInformation("HL7 Listener stopped");
    }

    // ── Per-connection handler ────────────────────────────────────────────────

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await _connectionSemaphore.WaitAsync(cancellationToken);
        Interlocked.Increment(ref _activeConnections);

        try
        {
            using (client)
            {
                var endpoint = client.Client.RemoteEndPoint?.ToString() ?? Hl7ProtocolConstants.UnknownEndpoint;
                _logger.LogInformation(
                    "HL7 client connected: {Endpoint} (active={Active})", endpoint, _activeConnections);

                var stream = client.GetStream();

                using var cts = new CancellationTokenSource(_options.ConnectionTimeoutMs);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

                using var scope = _serviceScopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IHl7MessageRepository>();
                var validator  = scope.ServiceProvider.GetRequiredService<IHl7ValidationService>();

                await foreach (var content in ReadMllpMessagesAsync(stream, endpoint, linked.Token))
                {
                    var message = Hl7Message.Create(content, endpoint, _options.Port);

                    // P0-5: Validate BEFORE ACK. Sending AA on an invalid message means the
                    // HIS never retransmits — silent PHI loss. When ValidateBeforeAck=true,
                    // invalid messages receive a NACK (AE) and are persisted with
                    // DispatchStatus=ValidationFailed so the dispatcher skips them.
                    if (_options.ValidateBeforeAck)
                    {
                        var validation = validator.Validate(message);
                        if (!validation.IsValid)
                        {
                            message.MarkAsValidationFailed(validation.ErrorMessage ?? "validation failed");
                            await repository.AddAsync(message, linked.Token);

                            _logger.LogWarning(
                                "HL7 message {MessageId} rejected at intake — {Error}",
                                message.Id, validation.ErrorMessage);

                            var nack = BuildHl7Nack(message,
                                Hl7ProtocolConstants.NackErrorCode,
                                validation.ErrorMessage ?? "validation failed");
                            await stream.WriteAsync(Encoding.UTF8.GetBytes(nack), linked.Token);
                            continue;
                        }
                    }

                    await repository.AddAsync(message, linked.Token);

                    _logger.LogInformation(
                        "HL7 message {MessageId} received — Type={MessageType} Trigger={Trigger} MRG={HasMrg}",
                        message.Id, message.MessageType, message.TriggerEvent, message.HasMrgSegment);

                    await _messageChannel.Writer.WriteAsync(message, linked.Token);

                    var ack = BuildHl7Ack(message);
                    await stream.WriteAsync(Encoding.UTF8.GetBytes(ack), linked.Token);
                }

                _logger.LogInformation("HL7 client disconnected: {Endpoint}", endpoint);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("HL7 connection timeout or cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling HL7 client");
        }
        finally
        {
            Interlocked.Decrement(ref _activeConnections);
            _connectionSemaphore.Release();
        }
    }

    // ── MLLP reader — yields one complete HL7 message per call ───────────────

    /// <summary>
    /// Reads bytes from <paramref name="stream"/> and yields one fully framed HL7 message
    /// each time the MLLP end-block (0x1C 0x0D) is detected.
    /// Handles partial TCP reads and buffers containing multiple messages.
    /// </summary>
    private async IAsyncEnumerable<string> ReadMllpMessagesAsync(
        NetworkStream stream,
        string endpoint,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer      = new byte[_options.BufferSize];
        var accumulator = new StringBuilder();
        bool inMessage  = false;
        var pending     = new Queue<string>();

        while (!ct.IsCancellationRequested)
        {
            // Yield any already-complete messages before blocking on the next read
            while (pending.TryDequeue(out var ready))
                yield return ready;

            int bytesRead;
            try
            {
                bytesRead = await stream.ReadAsync(buffer.AsMemory(), ct);
            }
            catch (OperationCanceledException) { yield break; }
            catch (IOException)                { yield break; }

            if (bytesRead == 0)
            {
                _logger.LogDebug("HL7 stream closed by {Endpoint}", endpoint);
                yield break;
            }

            for (int i = 0; i < bytesRead; i++)
            {
                var b = buffer[i];

                if (b == (byte)Hl7ProtocolConstants.StartBlock)
                {
                    // Start of a new MLLP message — reset accumulator
                    accumulator.Clear();
                    inMessage = true;
                }
                else if (b == (byte)Hl7ProtocolConstants.EndBlock && inMessage)
                {
                    // End-block found; consume the mandatory trailing CR if present
                    if (i + 1 < bytesRead && buffer[i + 1] == (byte)'\r')
                        i++;

                    inMessage = false;
                    var msgContent = accumulator.ToString();
                    accumulator.Clear();

                    if (!string.IsNullOrWhiteSpace(msgContent))
                        pending.Enqueue(msgContent);
                }
                else if (inMessage)
                {
                    accumulator.Append((char)b);
                }
                // bytes outside a message frame (e.g., stray keep-alive bytes) are discarded
            }
        }

        // Drain any messages completed in the last read
        while (pending.TryDequeue(out var last))
            yield return last;
    }

    // ── Message processor workers ─────────────────────────────────────────────

    private async Task ProcessMessagesAsync(int workerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HL7 processing worker {WorkerId} started", workerId);

        await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                _logger.LogDebug(
                    "Worker {WorkerId} processing {MessageId} ({MessageType}^{Trigger})",
                    workerId, message.Id, message.MessageType, message.TriggerEvent);

                using var scope = _serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IHl7MessageProcessor>();
                await processor.ProcessAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Worker {WorkerId} failed to process {MessageId}", workerId, message.Id);
            }
        }

        _logger.LogInformation("HL7 processing worker {WorkerId} stopped", workerId);
    }

    // ── ACK builder ───────────────────────────────────────────────────────────

    private string BuildHl7Ack(Hl7Message originalMessage)
    {
        var timestamp            = DateTime.Now.ToString("yyyyMMddHHmmss");
        var ackControlId         = Guid.NewGuid().ToString("N")[..10].ToUpper();
        var originalControlId    = originalMessage.MessageControlId ?? ackControlId;

        var ackSegments =
            $"MSH|^~\\&|{Hl7ProtocolConstants.SenderApplication}|{Hl7ProtocolConstants.SenderFacility}" +
            $"|{originalMessage.SendingApplication}|{originalMessage.SendingFacility}" +
            $"|{timestamp}||{Hl7ProtocolConstants.AckMessageType}" +
            $"|{ackControlId}|{Hl7ProtocolConstants.ProcessingId}|{Hl7ProtocolConstants.Hl7Version}" +
            $"{Hl7ProtocolConstants.SegmentTerminator}" +
            $"MSA|{Hl7ProtocolConstants.AckCode}|{originalControlId}" +
            $"{Hl7ProtocolConstants.SegmentTerminator}";

        return $"{Hl7ProtocolConstants.StartBlock}{ackSegments}{Hl7ProtocolConstants.EndBlock}{Hl7ProtocolConstants.SegmentTerminator}";
    }

    /// <summary>
    /// P0-5: Builds a NACK response with the given ACK code (AE = Application Error,
    /// AR = Application Reject) and an error text placed in MSA-3.
    /// HL7 separators in the error text are escaped per HL7 v2 spec.
    /// </summary>
    private string BuildHl7Nack(Hl7Message originalMessage, string nackCode, string errorText)
    {
        var timestamp         = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var ackControlId      = Guid.NewGuid().ToString("N")[..10].ToUpper();
        var originalControlId = originalMessage.MessageControlId ?? ackControlId;

        // Escape HL7 separators in the error text so it can never break the MSA segment.
        var safeError = (errorText ?? "validation failed")
            .Replace("|",  "\\F\\")
            .Replace("^",  "\\S\\")
            .Replace("~",  "\\R\\")
            .Replace("\\", "\\E\\")
            .Replace("\r", " ")
            .Replace("\n", " ");
        if (safeError.Length > 200) safeError = safeError[..200];

        var ackSegments =
            $"MSH|^~\\&|{Hl7ProtocolConstants.SenderApplication}|{Hl7ProtocolConstants.SenderFacility}" +
            $"|{originalMessage.SendingApplication}|{originalMessage.SendingFacility}" +
            $"|{timestamp}||{Hl7ProtocolConstants.AckMessageType}" +
            $"|{ackControlId}|{Hl7ProtocolConstants.ProcessingId}|{Hl7ProtocolConstants.Hl7Version}" +
            $"{Hl7ProtocolConstants.SegmentTerminator}" +
            $"MSA|{nackCode}|{originalControlId}|{safeError}" +
            $"{Hl7ProtocolConstants.SegmentTerminator}";

        return $"{Hl7ProtocolConstants.StartBlock}{ackSegments}{Hl7ProtocolConstants.EndBlock}{Hl7ProtocolConstants.SegmentTerminator}";
    }
}
