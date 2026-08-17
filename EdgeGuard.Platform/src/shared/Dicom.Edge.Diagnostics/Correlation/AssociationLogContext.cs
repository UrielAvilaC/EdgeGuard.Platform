namespace Dicom.Edge.Diagnostics.Correlation;

/// <summary>
/// Identity of a single DICOM association, used to route its log events to a dedicated
/// per-association file.
/// </summary>
/// <remarks>
/// Deliberately <b>mutable</b>: fo-dicom creates the SCP service instance (and therefore the
/// association id) before the A-ASSOCIATE-RQ is parsed, so the AE titles and remote endpoint
/// are only known once <c>OnReceiveAssociationRequestAsync</c> runs. The logger decorator
/// captures this object by reference, so events emitted before the AE titles are known still
/// carry the correct <see cref="AssociationId"/>.
/// </remarks>
public sealed class AssociationLogContext(string associationId)
{
    /// <summary>Short, unique identifier of the association (also used in the file name).</summary>
    public string AssociationId { get; } = associationId;

    /// <summary>UTC timestamp of the incoming connection.</summary>
    public DateTime ConnectedAt { get; } = DateTime.UtcNow;

    /// <summary>Calling AE title, set when the association request is received.</summary>
    public string CallingAe { get; set; } = Unknown;

    /// <summary>Called AE title, set when the association request is received.</summary>
    public string CalledAe { get; set; } = Unknown;

    /// <summary>Remote host/IP, set when the association request is received.</summary>
    public string RemoteHost { get; set; } = Unknown;

    /// <summary>Remote TCP port, set when the association request is received.</summary>
    public int RemotePort { get; set; }

    /// <summary>Placeholder used until the association request has been parsed.</summary>
    public const string Unknown = "?";

    /// <summary>Creates a context with a freshly generated short association id.</summary>
    public static AssociationLogContext New() =>
        new(Guid.NewGuid().ToString("N")[..8]);
}
