namespace Dicom.Edge.Node.Persistence.Entities;

/// <summary>
/// Marks an entity as having automatic timestamp management.
/// The <see cref="TimestampInterceptor"/> sets these fields before every save.
/// </summary>
public interface ITimestampedEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
