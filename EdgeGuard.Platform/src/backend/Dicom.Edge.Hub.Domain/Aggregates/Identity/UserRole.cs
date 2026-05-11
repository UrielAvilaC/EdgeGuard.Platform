using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Identity;

/// <summary>
/// Represents a role assignment for a user.
/// </summary>
public sealed class UserRole : Entity<string>
{
    public string UserId { get; private set; } = default!;
    public Security.Authorization.Role Role { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public string? AssignedBy { get; private set; }

    private UserRole() { }

    public static UserRole Create(
        string userId,
        Security.Authorization.Role role,
        string? assignedBy = null)
    {
        return new UserRole
        {
            Id = IdGenerator.NewId(),
            UserId = userId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
            AssignedBy = assignedBy
        };
    }
}
