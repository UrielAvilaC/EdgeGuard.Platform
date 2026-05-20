using Dicom.Edge.Contracts.Hl7;
using Dicom.Edge.Node.Persistence.Diagnostics;
using Dicom.Edge.Node.Worklist;
using DbWorklistItem = Dicom.Edge.Models.Worklist.WorklistItem;
using NodeWorklistItem = Dicom.Edge.Node.Worklist.WorklistItem;

namespace Dicom.Edge.Node.Persistence.Services;

/// <summary>
/// SQLite-backed implementation of <see cref="IWorklistManager"/>.
/// Persists HL7 worklist items pushed from the Hub so they survive node restarts.
/// Uses <see cref="IDbContextFactory{TContext}"/> for Singleton-safe DB access.
/// </summary>
public sealed class SqliteWorklistManager(
    IDbContextFactory<EdgeNodeDbContext> factory,
    ILogger<SqliteWorklistManager> logger) : IWorklistManager
{
    public async Task<WorklistAcceptResult> AcceptWorklistItemAsync(
        Hl7WorklistPushRequest request, CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await factory.CreateDbContextAsync(ct);

            // Upsert by AccessionNumber — if a worklist item with the same accession
            // already exists, update it (e.g., schedule change via HL7 SIU).
            var existing = await ctx.WorklistItems
                .FirstOrDefaultAsync(w => w.AccessionNumber == request.AccessionNumber, ct);

            if (existing is not null)
            {
                existing.PatientId = request.PatientId ?? existing.PatientId;
                existing.PatientName = request.PatientName ?? existing.PatientName;
                existing.Modality = request.Modality ?? existing.Modality;
                existing.ScheduledDate = request.ScheduledDateTime?.Date ?? existing.ScheduledDate;
                existing.ProcedureDescription = request.ProcedureDescription ?? existing.ProcedureDescription;

                // MWL-FIX-2: persist the fields that the C-FIND SCP filters on
                // and emits in the response.
                existing.PatientBirthDate                 = request.PatientBirthDate                 ?? existing.PatientBirthDate;
                existing.PatientSex                       = request.PatientSex                       ?? existing.PatientSex;
                existing.ScheduledStationAeTitle          = request.ScheduledStationAeTitle          ?? existing.ScheduledStationAeTitle;
                existing.ScheduledPerformingPhysicianName = request.ScheduledPerformingPhysicianName ?? existing.ScheduledPerformingPhysicianName;
                existing.ScheduledProcedureStepId         = request.ScheduledProcedureStepId         ?? existing.ScheduledProcedureStepId;
                existing.RequestedProcedureId             = request.RequestedProcedureId             ?? existing.RequestedProcedureId;
                existing.ReferringPhysicianName           = request.ReferringPhysicianName           ?? existing.ReferringPhysicianName;
                existing.StudyInstanceUid                 = request.StudyInstanceUid                 ?? existing.StudyInstanceUid;

                await ctx.SaveChangesAsync(ct);

                logger.LogInformation(
                    "Worklist item updated: {AccessionNumber} for patient {PatientId}",
                    existing.AccessionNumber, existing.PatientId);

                return new WorklistAcceptResult { Accepted = true, AckId = existing.AccessionNumber };
            }

            var item = new DbWorklistItem
            {
                AccessionNumber = request.AccessionNumber ?? Guid.NewGuid().ToString("N")[..16],
                PatientId = request.PatientId ?? string.Empty,
                PatientName = request.PatientName ?? string.Empty,
                Modality = request.Modality ?? "OT",
                ScheduledDate = request.ScheduledDateTime?.Date ?? DateTime.UtcNow.Date,
                ProcedureDescription = request.ProcedureDescription ?? string.Empty,

                // MWL-FIX-2: persist the fields the C-FIND SCP filters on and emits.
                PatientBirthDate                 = request.PatientBirthDate,
                PatientSex                       = request.PatientSex,
                ScheduledStationAeTitle          = request.ScheduledStationAeTitle,
                ScheduledPerformingPhysicianName = request.ScheduledPerformingPhysicianName,
                ScheduledProcedureStepId         = request.ScheduledProcedureStepId,
                RequestedProcedureId             = request.RequestedProcedureId,
                ReferringPhysicianName           = request.ReferringPhysicianName,
                StudyInstanceUid                 = request.StudyInstanceUid,
            };

            ctx.WorklistItems.Add(item);
            await ctx.SaveChangesAsync(ct);

            logger.LogInformation(
                "Worklist item accepted: {AccessionNumber} for patient {PatientId}",
                item.AccessionNumber, item.PatientId);

            return new WorklistAcceptResult { Accepted = true, AckId = item.AccessionNumber };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to accept worklist item for accession {Accession}",
                request.AccessionNumber);
            return new WorklistAcceptResult { Accepted = false, Error = ex.Message };
        }
    }

    public async Task<IReadOnlyList<NodeWorklistItem>> GetActiveItemsAsync(CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        var dbItems = await ctx.WorklistItems
            .Where(w => EF.Property<string>(w, "status") == "pending")
            .OrderBy(w => w.ScheduledDate)
            .ThenBy(w => w.PatientName)
            .AsNoTracking()
            .ToListAsync(ct);

        return dbItems.Select(MapToNodeWorklistItem).ToList();
    }

    public async Task<IReadOnlyList<NodeWorklistItem>> QueryAsync(
        DateTime? from, DateTime? to, string? modality, CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        IQueryable<DbWorklistItem> query = ctx.WorklistItems
            .Where(w => EF.Property<string>(w, "status") == "pending");

        if (from.HasValue)
            query = query.Where(w => w.ScheduledDate >= from.Value);

        if (to.HasValue)
            query = query.Where(w => w.ScheduledDate <= to.Value);

        if (!string.IsNullOrEmpty(modality))
            query = query.Where(w => w.Modality.ToUpper() == modality.ToUpper());

        var dbItems = await query
            .OrderBy(w => w.ScheduledDate)
            .AsNoTracking()
            .ToListAsync(ct);

        return dbItems.Select(MapToNodeWorklistItem).ToList();
    }

    public async Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);

        return await ctx.WorklistItems
            .CountAsync(w => EF.Property<string>(w, "status") == "pending", ct);
    }

    /// <summary>
    /// Maps the persistence model to the node-layer <see cref="NodeWorklistItem"/>.
    /// </summary>
    private static NodeWorklistItem MapToNodeWorklistItem(DbWorklistItem db) => new()
    {
        Id                = db.AccessionNumber,
        HubMessageId      = Guid.Empty,
        MessageType       = "ORM",
        AccessionNumber   = db.AccessionNumber,

        // Patient
        PatientId         = db.PatientId,
        PatientName       = db.PatientName,
        PatientBirthDate  = db.PatientBirthDate,
        PatientSex        = db.PatientSex,

        // Study / Requested Procedure
        StudyInstanceUid       = db.StudyInstanceUid,
        ReferringPhysicianName = db.ReferringPhysicianName,
        ProcedureDescription   = db.ProcedureDescription,
        RequestedProcedureId   = db.RequestedProcedureId,

        // Scheduled Procedure Step
        Modality                         = db.Modality,
        ScheduledDateTime                = db.ScheduledDate,
        ScheduledStationAeTitle          = db.ScheduledStationAeTitle,
        ScheduledPerformingPhysicianName = db.ScheduledPerformingPhysicianName,
        ScheduledProcedureStepId         = db.ScheduledProcedureStepId,

        // Lifecycle
        ReceivedAt   = db.ScheduledDate,
        ExpiresAt    = null,
        IsProcessed  = false,
    };

    public async Task MarkItemsAsQueriedAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return;

        await using var ctx = await factory.CreateDbContextAsync(ct);

        var items = await ctx.WorklistItems
            .Where(w => idList.Contains(w.AccessionNumber))
            .ToListAsync(ct);

        foreach (var item in items)
            ctx.Entry(item).Property("status").CurrentValue = "queried";

        await ctx.SaveChangesAsync(ct);

        logger.LogInformation("Marked {Count} worklist item(s) as queried", items.Count);
    }
}
