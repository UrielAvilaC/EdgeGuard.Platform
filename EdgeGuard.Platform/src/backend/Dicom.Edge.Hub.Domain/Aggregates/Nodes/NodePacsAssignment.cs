using Dicom.Edge.Hub.Domain.Common;

namespace Dicom.Edge.Hub.Domain.Aggregates.Nodes;

/// <summary>
/// Represents the assignment of a PACS server to a node, with C-ECHO tracking.
/// </summary>
public sealed class NodePacsAssignment : Entity<string>
{
    public string NodeId { get; private set; } = default!;
    public string PacsId { get; private set; } = default!;
    public DateTime AssignedAt { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this assignment was inherited from Hub-level global PACS config.
    /// </summary>
    public bool InheritedFromHub { get; private set; }

    // C-ECHO tracking
    public DateTime? LastCEchoAt { get; private set; }
    public bool? LastCEchoSuccess { get; private set; }
    public int CEchoIntervalSeconds { get; private set; }



    private NodePacsAssignment() { }

    public static NodePacsAssignment Create(
        string nodeId,
        string pacsId,
        bool inheritedFromHub = false,
        int cEchoIntervalSeconds = 300)
    {
        return new NodePacsAssignment
        {
            Id = IdGenerator.NewId(),
            NodeId = nodeId,
            PacsId = pacsId,
            AssignedAt = DateTime.UtcNow,
            IsActive = true,
            InheritedFromHub = inheritedFromHub,
            CEchoIntervalSeconds = cEchoIntervalSeconds
        };
    }

    public void UpdateCEchoResult(bool success)
    {
        LastCEchoAt = DateTime.UtcNow;
        LastCEchoSuccess = success;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
