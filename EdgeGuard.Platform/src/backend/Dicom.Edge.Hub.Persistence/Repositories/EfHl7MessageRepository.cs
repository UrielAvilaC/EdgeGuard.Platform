using Dicom.Edge.Hub.Domain.Entities;
using Dicom.Edge.Hub.Domain.Interfaces;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class EfHl7MessageRepository : IHl7MessageRepository
{
    private readonly HubDbContext _context;

    public EfHl7MessageRepository(HubDbContext context) => _context = context;

    public async Task<Hl7Message> AddAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        await _context.Hl7Messages.AddAsync(message, cancellationToken);
        return message;
    }

    public async Task<Hl7Message?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<Hl7Message>> GetByStatusAsync(
        Hl7MessageStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .Where(m => m.Status == status)
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Hl7Message>> GetByDispatchStatusAsync(
        Hl7DispatchStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .Where(m => m.DispatchStatus == status)
            .OrderByDescending(m => m.ReceivedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Hl7Message>> GetQueuedForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .Where(m => m.DispatchStatus == Hl7DispatchStatus.Queued)
            .OrderBy(m => m.Priority)
            .ThenBy(m => m.ReceivedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(Hl7Message message, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(message);

        if (entry.State == EntityState.Detached)
            _context.Hl7Messages.Update(message);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Hl7Message>> GetRecentMessagesAsync(
        int count,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .OrderByDescending(m => m.ReceivedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public async Task<int> CountByDispatchStatusAsync(
        Hl7DispatchStatus status,
        CancellationToken cancellationToken = default) =>
        await _context.Hl7Messages
            .CountAsync(m => m.DispatchStatus == status, cancellationToken);
}
