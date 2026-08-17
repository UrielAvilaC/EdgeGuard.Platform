using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NotificationTemplateRepository(HubDbContext context) : INotificationTemplateRepository
{
    public async Task<IReadOnlyList<NotificationTemplate>> GetAllAsync(CancellationToken ct = default) =>
        await context.NotificationTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<NotificationTemplate?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.NotificationTemplates.FindAsync([id], ct);

    public async Task<NotificationTemplate> AddAsync(NotificationTemplate template, CancellationToken ct = default)
    {
        await context.NotificationTemplates.AddAsync(template, ct);
        return template;
    }

    public Task UpdateAsync(NotificationTemplate template, CancellationToken ct = default)
    {
        context.NotificationTemplates.Update(template);
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await context.NotificationTemplates.FindAsync([id], ct);
        if (entity is null) return false;
        context.NotificationTemplates.Remove(entity);
        return true;
    }
}
