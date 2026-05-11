using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class WhatsAppNotificationRepository : IWhatsAppNotificationRepository
{
    private readonly HubDbContext _context;

    public WhatsAppNotificationRepository(HubDbContext context) => _context = context;

    public async Task<WhatsAppNotification> AddAsync(WhatsAppNotification notification, CancellationToken ct = default)
    {
        await _context.WhatsAppNotifications.AddAsync(notification, ct);
        return notification;
    }

    public Task UpdateAsync(WhatsAppNotification notification, CancellationToken ct = default)
    {
        _context.WhatsAppNotifications.Update(notification);
        return Task.CompletedTask;
    }

    public async Task<WhatsAppNotification?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _context.WhatsAppNotifications.FindAsync([id], ct);

    public async Task<IReadOnlyList<WhatsAppNotification>> GetByStudyAsync(string studyId, CancellationToken ct = default) =>
        await _context.WhatsAppNotifications
            .AsNoTracking()
            .Where(n => n.StudyId == studyId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WhatsAppNotification>> GetPendingAsync(int limit = 50, CancellationToken ct = default) =>
        await _context.WhatsAppNotifications
            .Where(n => n.Status == WhatsAppNotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WhatsAppNotification>> GetByStatusAsync(WhatsAppNotificationStatus status, int limit = 100, CancellationToken ct = default) =>
        await _context.WhatsAppNotifications
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
            deleted = await _context.WhatsAppNotifications
                .Where(n => n.CreatedAt < cutoff)
                .OrderBy(n => n.CreatedAt)
                .Take(batchSize)
                .ExecuteDeleteAsync(ct);
            total += deleted;
        } while (deleted == batchSize && !ct.IsCancellationRequested);
        return total;
    }
}
