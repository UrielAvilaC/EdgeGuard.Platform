using Dicom.Edge.Contracts.Hl7;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Node.Worklist;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IWorklistManager"/>.
/// Stores HL7 worklist items pushed by the Hub and serves them for MWL queries.
/// </summary>
public sealed class InMemoryWorklistManager(
    ILogger<InMemoryWorklistManager> logger) : IWorklistManager
{
    private readonly List<WorklistItem> _items = [];
    private readonly Lock _lock = new();

    public Task<WorklistAcceptResult> AcceptWorklistItemAsync(
        Hl7WorklistPushRequest request, CancellationToken ct = default)
    {
        var item = new WorklistItem
        {
            Id = Guid.NewGuid().ToString(),
            HubMessageId = request.HubMessageId,
            MessageType = request.MessageType,

            // Patient
            PatientId = request.PatientId,
            PatientName = request.PatientName,
            PatientBirthDate = request.PatientBirthDate,
            PatientSex = request.PatientSex,

            // Study / Requested Procedure
            AccessionNumber = request.AccessionNumber,
            StudyInstanceUid = request.StudyInstanceUid,
            ReferringPhysicianName = request.ReferringPhysicianName,
            ProcedureDescription = request.ProcedureDescription,
            RequestedProcedureId = request.RequestedProcedureId,

            // Scheduled Procedure Step
            Modality = request.Modality,
            ScheduledDateTime = request.ScheduledDateTime,
            ScheduledStationAeTitle = request.ScheduledStationAeTitle,
            ScheduledPerformingPhysicianName = request.ScheduledPerformingPhysicianName,
            ScheduledProcedureStepId = request.ScheduledProcedureStepId,

            // HL7 metadata
            SendingFacility = request.SendingFacility,
            RawContent = request.RawContent,
            Priority = request.Priority,
            ReceivedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        lock (_lock)
        {
            _items.Add(item);
        }

        logger.LogInformation("Worklist item accepted: {AckId} for patient {PatientId}",
            item.Id, item.PatientId);

        return Task.FromResult(new WorklistAcceptResult { Accepted = true, AckId = item.Id });
    }

    public Task<IReadOnlyList<WorklistItem>> GetActiveItemsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var active = _items
                .Where(i => !i.IsProcessed && (i.ExpiresAt is null || i.ExpiresAt > DateTime.UtcNow))
                .OrderBy(i => i.Priority)
                .ThenBy(i => i.ReceivedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<WorklistItem>>(active);
        }
    }

    public Task<IReadOnlyList<WorklistItem>> QueryAsync(
        DateTime? from, DateTime? to, string? modality, CancellationToken ct = default)
    {
        lock (_lock)
        {
            IEnumerable<WorklistItem> query = _items.Where(i => !i.IsProcessed);

            if (from.HasValue)
                query = query.Where(i => i.ReceivedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(i => i.ReceivedAt <= to.Value);

            return Task.FromResult<IReadOnlyList<WorklistItem>>(query.ToList());
        }
    }

    public Task<int> GetActiveCountAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var count = _items.Count(i =>
                !i.IsProcessed && (i.ExpiresAt is null || i.ExpiresAt > DateTime.UtcNow));

            return Task.FromResult(count);
        }
    }
}
