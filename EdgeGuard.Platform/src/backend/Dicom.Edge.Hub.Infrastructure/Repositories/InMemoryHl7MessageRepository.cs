using System.Collections.Concurrent;
using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;

namespace Dicom.Edge.Hub.Infrastructure.Repositories;

/// <summary>
/// Implementación en memoria del repositorio de mensajes HL7.
/// NOTA: Para producción, reemplazar con una implementación persistente (SQL, MongoDB, etc.)
/// </summary>
public class InMemoryHl7MessageRepository : IHl7MessageRepository
{
    private readonly ConcurrentDictionary<Guid, Hl7Message> _messages = new();

    public Task<Hl7Message> AddAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        _messages.TryAdd(message.Id, message);
        return Task.FromResult(message);
    }

    public Task<Hl7Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _messages.TryGetValue(id, out var message);
        return Task.FromResult(message);
    }

    public Task<IEnumerable<Hl7Message>> GetByStatusAsync(
        Hl7MessageStatus status, 
        CancellationToken cancellationToken = default)
    {
        var messages = _messages.Values.Where(m => m.Status == status).ToList();
        return Task.FromResult<IEnumerable<Hl7Message>>(messages);
    }

    public Task UpdateAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        _messages[message.Id] = message;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Hl7Message>> GetRecentMessagesAsync(
        int count, 
        CancellationToken cancellationToken = default)
    {
        var messages = _messages.Values
            .OrderByDescending(m => m.ReceivedAt)
            .Take(count)
            .ToList();
        return Task.FromResult<IEnumerable<Hl7Message>>(messages);
    }
}
