using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Application.Hl7;

/// <summary>
/// Implementación del procesador de mensajes HL7.
/// </summary>
public class Hl7MessageProcessor : IHl7MessageProcessor
{
    private readonly IHl7MessageRepository _repository;
    private readonly ILogger<Hl7MessageProcessor> _logger;

    public Hl7MessageProcessor(
        IHl7MessageRepository repository,
        ILogger<Hl7MessageProcessor> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task ProcessAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing HL7 message {MessageId} of type {MessageType} from {Client}",
                message.Id,
                message.MessageType,
                message.ClientEndpoint);

            message.MarkAsProcessing();
            await _repository.UpdateAsync(message, cancellationToken);

            // AQUÍ IMPLEMENTA TU LÓGICA DE NEGOCIO
            // Ejemplos:
            // - Parsear el mensaje HL7 completo
            // - Validar campos requeridos
            // - Mapear a entidades del dominio
            // - Enviar a otros sistemas
            // - Generar eventos de integración

            await Task.Delay(100, cancellationToken); // Simular procesamiento

            message.MarkAsProcessed();
            await _repository.UpdateAsync(message, cancellationToken);

            _logger.LogInformation(
                "Successfully processed HL7 message {MessageId}",
                message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing HL7 message {MessageId}",
                message.Id);

            message.MarkAsFailed(ex.Message);
            await _repository.UpdateAsync(message, cancellationToken);

            throw;
        }
    }
}
