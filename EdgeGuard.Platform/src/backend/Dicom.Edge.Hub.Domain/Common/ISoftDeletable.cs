namespace Dicom.Edge.Hub.Domain.Common;

/// <summary>
/// Marks an entity as supporting soft-delete.
/// Entities implementing this interface are excluded from queries via global query filters
/// when <see cref="IsDeleted"/> is <c>true</c>.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }

    void SoftDelete();
    void Restore();
}
