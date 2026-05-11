using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Identity;

/// <summary>
/// Represents a direct permission override for a user (grant or deny).
/// </summary>
public sealed class UserPermission : Entity<string>
{
    public string UserId { get; private set; } = default!;
    public Security.Authorization.Permission Permission { get; private set; }
    public bool IsGranted { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public string? GrantedBy { get; private set; }

    private UserPermission() { }

    public static UserPermission Create(
        string userId,
        Security.Authorization.Permission permission,
        bool isGranted,
        string? grantedBy = null)
    {
        return new UserPermission
        {
            Id = IdGenerator.NewId(),
            UserId = userId,
            Permission = permission,
            IsGranted = isGranted,
            GrantedAt = DateTime.UtcNow,
            GrantedBy = grantedBy
        };
    }
}
