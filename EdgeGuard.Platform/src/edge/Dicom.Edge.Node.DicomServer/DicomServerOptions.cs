namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Configuration options for the DICOM SCP server (C-STORE + MWL C-FIND).
/// </summary>
public sealed class DicomServerOptions
{
    public const string SectionName = "DicomServer";

    public bool Enabled { get; set; } = true;
    public string AeTitle { get; set; } = "EDGENODE";
    public int Port { get; set; } = 11112;
    public int MaxClients { get; set; } = 10;
    public int AssociationTimeoutSeconds { get; set; } = 30;
    public int DimseTimeoutSeconds { get; set; } = 600;
    public int MaxPduLength { get; set; } = 262144;
    public string[] AllowedCallingAeTitles { get; set; } = [];

    /// <summary>
    /// Additional AE titles this node accepts as CalledAE besides <see cref="AeTitle"/>.
    /// Useful when clients still target the node's old AE title after a rename,
    /// or when multiple logical names must be served from a single port.
    /// </summary>
    public string[] AeTitleAliases { get; set; } = [];

    /// <summary>
    /// When <c>true</c> (default), incoming associations are rejected if the CalledAE
    /// does not match <see cref="AeTitle"/> or any entry in <see cref="AeTitleAliases"/>.
    /// Set to <c>false</c> to accept any CalledAE (permissive mode).
    /// </summary>
    public bool ValidateCalledAe { get; set; } = true;

    /// <summary>
    /// Enables Modality Worklist (MWL) C-FIND SCP on the same DICOM port.
    /// </summary>
    public bool MwlEnabled { get; set; } = true;

    /// <summary>
    /// Enables C-ECHO (Verification SCP) on the same DICOM port.
    /// </summary>
    public bool CEchoEnabled { get; set; } = true;

    /// <summary>
    /// Enables the Query/Retrieve (Q/R) SCP for Study Root and Patient Root C-FIND and C-MOVE.
    /// When enabled, remote SCUs can query the local study database and retrieve studies.
    /// </summary>
    public bool QrEnabled { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, only CallingAE titles listed in
    /// <see cref="AllowedCallingAeTitles"/> are accepted.
    /// Has no effect when <see cref="AllowedCallingAeTitles"/> is empty.
    /// </summary>
    public bool ValidateCallingAe { get; set; } = false;

    /// <summary>
    /// Returns true when <paramref name="calledAe"/> is accepted by this node,
    /// honouring <see cref="ValidateCalledAe"/>, <see cref="AeTitle"/> and
    /// <see cref="AeTitleAliases"/>.
    /// </summary>
    public bool IsAcceptedCalledAe(string calledAe) =>
        !ValidateCalledAe ||
        string.Equals(calledAe, AeTitle.Trim(), StringComparison.OrdinalIgnoreCase) ||
        AeTitleAliases.Contains(calledAe, StringComparer.OrdinalIgnoreCase);
}
