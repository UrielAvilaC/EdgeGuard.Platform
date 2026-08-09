using Dicom.Edge.Diagnostics.Correlation;

namespace Dicom.Edge.Diagnostics.Logging;

/// <summary>
/// Owns the per-association log files: opens one when an association arrives and closes it
/// deterministically on release, abort or connection close.
/// </summary>
/// <remarks>
/// Implementations must never throw: a logging failure must not break an association.
/// </remarks>
public interface IAssociationLogWriter
{
    /// <summary>
    /// Opens the log file for the association. Idempotent — a second call for the same
    /// association id is ignored.
    /// </summary>
    /// <returns>
    /// <c>true</c> when a file was opened (or was already open); <c>false</c> when per-association
    /// logging is disabled, a cap was hit, or the file could not be created.
    /// </returns>
    bool Open(AssociationLogContext context);

    /// <summary>
    /// Writes the summary footer, flushes and releases the file handle. Idempotent.
    /// </summary>
    void Close(AssociationLogContext context, AssociationSummary summary);

    /// <summary>
    /// Closes association files older than <paramref name="ttl"/> whose close callback never
    /// arrived, marking them as <see cref="AssociationOutcome.Orphaned"/>.
    /// </summary>
    /// <returns>Number of files closed.</returns>
    int SweepStale(TimeSpan ttl);
}
