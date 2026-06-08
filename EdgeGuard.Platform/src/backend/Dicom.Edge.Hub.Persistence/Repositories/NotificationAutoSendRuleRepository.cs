using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class NotificationAutoSendRuleRepository(HubDbContext context) : INotificationAutoSendRuleRepository
{
    public async Task<NotificationAutoSendRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.NotificationAutoSendRules.FindAsync([id], ct);

    public async Task<NotificationAutoSendRule?> GetByStudyStatusAsync(string studyStatus, CancellationToken ct = default) =>
        await context.NotificationAutoSendRules
            .FirstOrDefaultAsync(r => r.StudyStatus == studyStatus, ct);

    public async Task<IReadOnlyList<NotificationAutoSendRule>> GetAllAsync(CancellationToken ct = default) =>
        await context.NotificationAutoSendRules
            .OrderBy(r => r.StudyStatus)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<NotificationAutoSendRule>> GetEnabledAsync(CancellationToken ct = default) =>
        await context.NotificationAutoSendRules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.StudyStatus)
            .ToListAsync(ct);

    public async Task<NotificationAutoSendRule> AddAsync(NotificationAutoSendRule rule, CancellationToken ct = default)
    {
        await context.NotificationAutoSendRules.AddAsync(rule, ct);
        return rule;
    }

    public Task UpdateAsync(NotificationAutoSendRule rule, CancellationToken ct = default)
    {
        context.NotificationAutoSendRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await context.NotificationAutoSendRules.FindAsync([id], ct);
        if (entity is not null)
            context.NotificationAutoSendRules.Remove(entity);
    }
}
