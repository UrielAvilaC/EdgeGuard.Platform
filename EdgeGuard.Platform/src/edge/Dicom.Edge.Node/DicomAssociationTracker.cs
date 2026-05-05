using Dicom.Edge.Abstractions.Persistence;
using Dicom.Edge.Models.Dicom;
using Dicom.Edge.Models.Enums;
using Dicom.Edge.Node.DicomServer;
using Dicom.Edge.Node.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dicom.Edge.Node;

/// <summary>
/// Persists <see cref="Models.Dicom.DicomAssociation"/> records for every accepted and
/// rejected DICOM association. Records are accumulated in memory during the association
/// lifetime and written to SQLite in a single <c>INSERT</c> on finalization, following
/// the same scope-per-call pattern used by <see cref="DicomInstanceHandler"/>.
/// </summary>
internal sealed class DicomAssociationTracker(
    IServiceScopeFactory scopeFactory,
    INodeSettingsService settings,
    ILogger<DicomAssociationTracker> logger) : IDicomAssociationTracker
{
    /// <inheritdoc />
    public async Task<IAssociationSession> BeginAsync(
        string callingAe,
        string calledAe,
        string remoteHost,
        int    remotePort,
        string? acceptedContexts,
        CancellationToken ct = default)
    {
        var cfg    = await settings.GetGeneralConfigAsync(ct);
        var connectedAt = DateTime.UtcNow;

        return new AssociationSession(
            callingAe, calledAe, remoteHost, remotePort,
            acceptedContexts, connectedAt, cfg.NodeName,
            scopeFactory, logger);
    }

    /// <inheritdoc />
    public async Task RecordRejectionAsync(
        string callingAe,
        string calledAe,
        string remoteHost,
        int    remotePort,
        string reason,
        CancellationToken ct = default)
    {
        try
        {
            var cfg = await settings.GetGeneralConfigAsync(ct);
            var now = DateTime.UtcNow;

            await using var scope = scopeFactory.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<EdgeNodeDbContext>();

            ctx.Associations.Add(new DicomAssociation
            {
                CallingAeTitle = callingAe,
                CalledAeTitle  = calledAe,
                RemoteIpAddress = remoteHost,
                RemotePort     = remotePort,
                ConnectedAt    = now,
                DisconnectedAt = now,
                Status         = AssociationStatus.Rejected,
                RejectionReason = reason,
                EdgeNodeId     = cfg.NodeName,
            });

            await ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to persist rejected association from CallingAE={CallingAe}", callingAe);
        }
    }
}

/// <summary>
/// In-memory accumulator for a single accepted DICOM association.
/// Inserts one <see cref="Models.Dicom.DicomAssociation"/> row on first finalization call;
/// subsequent calls (e.g. release followed by close) are silently ignored.
/// </summary>
file sealed class AssociationSession(
    string callingAe,
    string calledAe,
    string remoteHost,
    int    remotePort,
    string? acceptedContexts,
    DateTime connectedAt,
    string? nodeId,
    IServiceScopeFactory scopeFactory,
    ILogger logger) : IAssociationSession
{
    private int _images;
    private int _finalized;

    /// <inheritdoc />
    public void RecordImage() => Interlocked.Increment(ref _images);

    /// <inheritdoc />
    public Task CompleteAsync(CancellationToken ct = default)
        => FinalizeAsync(AssociationStatus.Completed, null, ct);

    /// <inheritdoc />
    public Task AbortAsync(string? reason, CancellationToken ct = default)
        => FinalizeAsync(AssociationStatus.Aborted, reason, ct);

    private async Task FinalizeAsync(AssociationStatus status, string? reason, CancellationToken ct)
    {
        // Guard: only the first caller persists; subsequent calls (e.g. abort + close) are no-ops.
        if (Interlocked.Exchange(ref _finalized, 1) != 0) return;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var ctx = scope.ServiceProvider.GetRequiredService<EdgeNodeDbContext>();

            ctx.Associations.Add(new DicomAssociation
            {
                CallingAeTitle  = callingAe,
                CalledAeTitle   = calledAe,
                RemoteIpAddress = remoteHost,
                RemotePort      = remotePort,
                ConnectedAt     = connectedAt,
                DisconnectedAt  = DateTime.UtcNow,
                Status          = status,
                ImagesReceived  = _images,
                RejectionReason = reason,
                EdgeNodeId      = nodeId,
                AcceptedPresentationContexts = acceptedContexts,
            });

            await ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to persist association record — CallingAE={CallingAe} Status={Status}",
                callingAe, status);
        }
    }
}
