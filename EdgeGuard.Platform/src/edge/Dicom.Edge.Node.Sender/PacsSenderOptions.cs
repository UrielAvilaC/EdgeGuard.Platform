using Dicom.Edge.Abstractions.Configuration;

namespace Dicom.Edge.Node.Sender;

/// <summary>
/// Configuration options for the DICOM C-STORE SCU (sender).
/// </summary>
public sealed class PacsSenderOptions
{
    public const string SectionName = "PacsSender";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Outbound SCU Calling AE. Always DERIVED from the canonical node AE
    /// (<c>DicomServer:AeTitle</c>) via PostConfigure — this default is only a
    /// fallback. See <see cref="NodeAeTitle"/>.
    /// </summary>
    public string LocalAeTitle { get; set; } = NodeAeTitle.Default;
    public int MaxConcurrentSends { get; set; } = 4;
    public int TimeoutSeconds { get; set; } = 120;
    public int MaxRetries { get; set; } = 3;
    public int RetryBaseDelaySeconds { get; set; } = 10;
    public int ProcessingIntervalSeconds { get; set; } = 5;
}
