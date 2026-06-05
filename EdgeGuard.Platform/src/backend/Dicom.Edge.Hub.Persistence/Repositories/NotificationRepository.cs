using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly HubDbContext _context;

    public NotificationRepository(HubDbContext context) => _context = context;

    public async Task<Notification> AddAsync(Notification notification, CancellationToken ct = default)
    {
        await _context.Notifications.AddAsync(notification, ct);
        return notification;
    }

    public Task UpdateAsync(Notification notification, CancellationToken ct = default)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<Notification?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.Notifications.FindAsync([id], ct);

    public async Task<IReadOnlyList<Notification>> GetByStudyAsync(string studyId, CancellationToken ct = default) =>
        await _context.Notifications
            .AsNoTracking()
            .Where(n => n.StudyId == studyId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(int limit = 50, CancellationToken ct = default) =>
        await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> GetDuePendingAsync(int limit = 50, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending
                     && (n.NextAttemptAt == null || n.NextAttemptAt <= now))
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> GetByStatusAsync(NotificationStatus status, int limit = 100, CancellationToken ct = default) =>
        await _context.Notifications
            .AsNoTracking()
            .Where(n => n.Status == status)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize = 1000, CancellationToken ct = default)
    {
        var total = 0;
        int deleted;
        do
        {
            deleted = await _context.Notifications
                .Where(n => n.CreatedAt < cutoff)
                .OrderBy(n => n.CreatedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
