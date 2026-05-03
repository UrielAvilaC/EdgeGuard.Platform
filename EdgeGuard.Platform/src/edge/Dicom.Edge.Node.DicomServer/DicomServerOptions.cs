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
    /// Enables Modality Worklist (MWL) C-FIND SCP on the same DICOM port.
    /// When <c>false</c>, MWL presentation contexts are rejected.
    /// </summary>
    public bool MwlEnabled { get; set; } = true;

    /// <summary>
    /// Enables C-ECHO (Verification SCP) on the same DICOM port.
    /// When <c>true</c>, remote systems can ping the node with a DICOM echo.
    /// </summary>
    public bool CEchoEnabled { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, only CallingAE titles listed in
    /// <see cref="AllowedCallingAeTitles"/> are accepted.
    /// Has no effect when <see cref="AllowedCallingAeTitles"/> is empty.
    /// </summary>
    public bool ValidateCallingAe { get; set; } = false;
}
