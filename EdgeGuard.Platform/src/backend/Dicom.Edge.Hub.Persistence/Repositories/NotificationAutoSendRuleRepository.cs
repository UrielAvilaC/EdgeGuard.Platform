using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class WhatsAppAutoSendRuleRepository(HubDbContext context) : IWhatsAppAutoSendRuleRepository
{
    public async Task<WhatsAppAutoSendRule?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.WhatsAppAutoSendRules.FindAsync([id], ct);

    public async Task<WhatsAppAutoSendRule?> GetByStudyStatusAsync(string studyStatus, CancellationToken ct = default) =>
        await context.WhatsAppAutoSendRules
            .FirstOrDefaultAsync(r => r.StudyStatus == studyStatus, ct);

    public async Task<IReadOnlyList<WhatsAppAutoSendRule>> GetAllAsync(CancellationToken ct = default) =>
        await context.WhatsAppAutoSendRules
            .OrderBy(r => r.StudyStatus)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WhatsAppAutoSendRule>> GetEnabledAsync(CancellationToken ct = default) =>
        await context.WhatsAppAutoSendRules
            .Where(r => r.IsEnabled)
            .OrderBy(r => r.StudyStatus)
            .ToListAsync(ct);

    public async Task<WhatsAppAutoSendRule> AddAsync(WhatsAppAutoSendRule rule, CancellationToken ct = default)
    {
        await context.WhatsAppAutoSendRules.AddAsync(rule, ct);
        return rule;
    }

    public Task UpdateAsync(WhatsAppAutoSendRule rule, CancellationToken ct = default)
    {
        context.WhatsAppAutoSendRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await context.WhatsAppAutoSendRules.FindAsync([id], ct);
        if (entity is not null)
            context.WhatsAppAutoSendRules.Remove(entity);
    }
}
