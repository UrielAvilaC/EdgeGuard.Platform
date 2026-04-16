using Dicom.Edge.Hub.Domain.Aggregates.Notifications;
using Dicom.Edge.Hub.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

public sealed class WhatsAppTemplateRepository(HubDbContext context) : IWhatsAppTemplateRepository
{
    public async Task<WhatsAppTemplate?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await context.WhatsAppTemplates.FindAsync([id], ct);

    public async Task<WhatsAppTemplate?> GetByIdWithVariablesAsync(string id, CancellationToken ct = default) =>
        await context.WhatsAppTemplates
            .Include(t => t.Variables.OrderBy(v => v.Position))
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<WhatsAppTemplate>> GetAllAsync(CancellationToken ct = default) =>
        await context.WhatsAppTemplates
            .Include(t => t.Variables.OrderBy(v => v.Position))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WhatsAppTemplate>> GetActiveAsync(CancellationToken ct = default) =>
        await context.WhatsAppTemplates
            .Include(t => t.Variables.OrderBy(v => v.Position))
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    public async Task<WhatsAppTemplate> AddAsync(WhatsAppTemplate template, CancellationToken ct = default)
    {
        await context.WhatsAppTemplates.AddAsync(template, ct);
        return template;
    }

    public Task UpdateAsync(WhatsAppTemplate template, CancellationToken ct = default)
    {
        context.WhatsAppTemplates.Update(template);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await context.WhatsAppTemplates.FindAsync([id], ct);
        if (entity is not null)
            context.WhatsAppTemplates.Remove(entity);
    }
}
