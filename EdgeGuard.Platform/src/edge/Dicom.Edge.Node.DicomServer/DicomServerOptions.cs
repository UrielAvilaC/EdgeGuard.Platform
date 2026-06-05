using Dicom.Edge.Abstractions.Configuration;

namespace Dicom.Edge.Node.DicomServer;

/// <summary>
/// Configuration options for the DICOM SCP server (C-STORE + MWL C-FIND).
/// </summary>
public sealed class DicomServerOptions
{
    public const string SectionName = "DicomServer";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The node's DICOM AE Title — the SINGLE SOURCE OF TRUTH for node identity.
    /// The SCU Calling AE (<c>PacsSender:LocalAeTitle</c>) and the Hub-registration AE
    /// (<c>HubConnection:AeTitle</c>) are derived from this. See <see cref="NodeAeTitle"/>.
    /// </summary>
    public string AeTitle { get; set; } = NodeAeTitle.Default;
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
    /// Enables Query/Retrieve (Q/R) Study Root C-FIND SCP support.
    /// When enabled, remote SCUs can query the local study database via C-FIND.
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

    /// <summary>
    /// P0-3: DICOM TLS configuration for the SCP listener (incoming associations).
    /// When <c>Tls.Enabled = true</c>, the listener wraps the TCP socket with TLS
    /// using the provided certificate. PHI in transit between modalities and the
    /// Edge Node is encrypted.
    /// </summary>
    public DicomTlsOptions Tls { get; set; } = new();
}

/// <summary>P0-3: TLS configuration for the DICOM SCP listener.</summary>
public sealed class DicomTlsOptions
{
    /// <summary>Master switch — when false (default) the SCP listens in plain TCP.</summary>
    public bool Enabled { get; set; }

    /// <summary>Path to the PFX/PEM file containing the server certificate and private key.</summary>
    public string? CertificatePath { get; set; }

    /// <summary>Password for the certificate file (if any).</summary>
    public string? CertificatePassword { get; set; }

    /// <summary>When true, the SCP requires mutual TLS — the SCU must present a client cert.</summary>
    public bool RequireClientCertificate { get; set; }
}
