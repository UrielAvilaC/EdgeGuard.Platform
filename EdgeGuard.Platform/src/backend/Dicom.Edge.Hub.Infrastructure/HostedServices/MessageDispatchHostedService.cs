using Dicom.Edge.Hub.Application.Dispatch;
using Dicom.Edge.Hub.Application.Queue;
using Dicom.Edge.Hub.Domain.Aggregates.Nodes;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Infrastructure.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dicom.Edge.Hub.Infrastructure.HostedServices;

public sealed class MessageDispatchHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessageDispatchHostedService> _logger;
    private readonly MessageQueueOptions _options;

    public MessageDispatchHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<MessageQueueOptions> options,
        ILogger<MessageDispatchHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.DispatchEnabled)
        {
            _logger.LogInformation("Message dispatch worker is disabled");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.DispatchIntervalSeconds));

        _logger.LogInformation(
            "Message dispatch worker started — batch={BatchSize} interval={IntervalSeconds}s maxRetries={MaxRetries}",
            _options.DispatchBatchSize, interval.TotalSeconds, _options.MaxRetries);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message dispatch iteration failed");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }

        _logger.LogInformation("Message dispatch worker stopped");
    }

    private async Task DispatchBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var messageRepo = scope.ServiceProvider.GetRequiredService<IHl7MessageRepository>();
        var nodeRepo = scope.ServiceProvider.GetRequiredService<INodeRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<INodeDispatcher>();

        var batch = (await messageRepo.GetQueuedForDispatchAsync(_options.DispatchBatchSize, ct)).ToList();

        if (batch.Count == 0)
            return;

        _logger.LogDebug("Dispatching {Count} queued messages", batch.Count);

        foreach (var message in batch)
        {
            try
            {
                var node = await nodeRepo.GetByIdAsync(message.TargetNodeId!, ct);

                if (node is null || string.IsNullOrWhiteSpace(node.ApiEndpoint))
                {
                    _logger.LogWarning(
                        "Node {NodeId} not found or has no API endpoint for message {MessageId}",
                        message.TargetNodeId, message.Id);
                    message.MarkAsDeliveryFailed(DispatchConstants.TargetNodeNotFoundMessage);
                    await messageRepo.UpdateAsync(message, ct);
                    continue;
                }

                message.MarkAsDispatching();
                await messageRepo.UpdateAsync(message, ct);

                var request = new NodeDispatchRequest
                {
                    MessageId = message.Id,
                    TargetNodeId = message.TargetNodeId!,
                    NodeApiEndpoint = node.ApiEndpoint,
                    MessageType = message.MessageType,
                    TriggerEvent = message.TriggerEvent,
                    Content = message.Content,
                    PatientId = message.PatientId,
                    PatientName = message.PatientName,
                    AccessionNumber = message.AccessionNumber,
                    SendingFacility = message.SendingFacility,
                    SendingApplication = message.SendingApplication,
                    Priority = message.Priority
                };

                var result = await dispatcher.DispatchAsync(request, ct);

                if (result.Success)
                {
                    message.MarkAsDelivered();
                }
                else if (message.DispatchAttempts >= _options.MaxRetries)
                {
                    message.MarkAsDeliveryFailed(
                        string.Format(DispatchConstants.MaxRetriesExceededTemplate, result.Error));
                }
                else
                {
                    message.RequeueForDispatch();
                    _logger.LogWarning(
                        "Message {MessageId} dispatch failed (attempt {Attempt}/{Max}): {Error}",
                        message.Id, message.DispatchAttempts, _options.MaxRetries, result.Error);
                }

                await messageRepo.UpdateAsync(message, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching message {MessageId}", message.Id);
                message.MarkAsDeliveryFailed(ex.Message);
                await messageRepo.UpdateAsync(message, ct);
            }
        }
    }
}
