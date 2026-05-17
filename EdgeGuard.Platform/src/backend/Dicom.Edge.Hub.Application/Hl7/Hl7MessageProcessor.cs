using Dicom.Edge.Hub.Application.Hl7.Pipeline;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Enterprise HL7 message processing pipeline.
/// Flow: Receive → Validate → Persist → Route → Queue for dispatch.
/// </summary>
public class Hl7MessageProcessor : IHl7MessageProcessor
{
    private readonly IHl7MessageRepository _repository;
    private readonly IHl7ValidationService _validationService;
    private readonly IHl7RoutingEngine _routingEngine;
    private readonly IHl7PatientSyncService _patientSyncService;
    private readonly IHl7StudySyncService _studySyncService;
    private readonly ILogger<Hl7MessageProcessor> _logger;

    public Hl7MessageProcessor(
        IHl7MessageRepository repository,
        IHl7ValidationService validationService,
        IHl7RoutingEngine routingEngine,
        IHl7PatientSyncService patientSyncService,
        IHl7StudySyncService studySyncService,
        ILogger<Hl7MessageProcessor> logger)
    {
        _repository = repository;
        _validationService = validationService;
        _routingEngine = routingEngine;
        _patientSyncService = patientSyncService;
        _studySyncService = studySyncService;
        _logger = logger;
    }

    public async Task ProcessAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Pipeline start: {MessageId} | Type={MessageType} TriggerEvent={TriggerEvent} From={Client} " +
                "PatientId={PatientId} Facility={Facility}",
                message.Id, message.MessageType, message.TriggerEvent,
                message.ClientEndpoint, message.PatientId, message.SendingFacility);

            message.MarkAsProcessing();
            await _repository.UpdateAsync(message, cancellationToken);

            // ── Step 1: Enterprise validation ─────────────────────────────────
            var validation = _validationService.Validate(message);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validation failed for {MessageId}: {Error}",
                    message.Id, validation.ErrorMessage);

                message.MarkAsValidationFailed(validation.ErrorMessage!);
                message.MarkAsFailed($"Validation: {validation.ErrorMessage}");
                await _repository.UpdateAsync(message, cancellationToken);
                return;
            }

            message.MarkAsValidated();

            if (validation.Warnings.Count > 0)
            {
                _logger.LogWarning(
                    "Validation warnings for {MessageId}: {Warnings}",
                    message.Id, string.Join("; ", validation.Warnings));
            }

            // ── Step 1.5: Sync patient and study from HL7 segments ────────────
            try
            {

                await _patientSyncService.SyncFromHl7Async(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Patient sync failed for {MessageId}, continuing pipeline", message.Id);
            }

            try
            {
                _logger.LogInformation("Starting study sync for {MessageId}", message.Id);
                await _studySyncService.SyncFromHl7Async(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Study sync failed for {MessageId}, continuing pipeline", message.Id);
            }

            // ── Step 2: Route to target node ──────────────────────────────────
            var route = await _routingEngine.EvaluateAsync(message, cancellationToken);

            if (!route.IsRouted)
            {
                _logger.LogWarning(
                    "No route found for {MessageId}: {Reason}. Message will remain queued for future routing.",
                    message.Id, route.Reason);

                message.MarkAsProcessed();
                await _repository.UpdateAsync(message, cancellationToken);
                return;
            }

            message.MarkAsRouted(route.TargetNodeId!, route.TargetNodeName, route.Priority);

            // ── Step 3: Queue for dispatch ────────────────────────────────────
            message.MarkAsQueued();
            message.MarkAsProcessed();
            await _repository.UpdateAsync(message, cancellationToken);

            _logger.LogInformation(
                "Pipeline complete: {MessageId} → Node={TargetNode} Priority={Priority} Rule={RuleId}",
                message.Id, route.TargetNodeName, route.Priority, route.MatchedRuleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline error for {MessageId}", message.Id);
            message.MarkAsFailed(ex.Message);
            await _repository.UpdateAsync(message, cancellationToken);
            throw;
        }
    }
}
