namespace Dicom.Edge.Hub.Application.Notifications;

/// <summary>
/// Generates QR code images (PNG) for a given payload — typically the study's
/// image-viewer link, used in the report viewer and results delivery.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>Renders <paramref name="content"/> as a PNG QR code.</summary>
    byte[] GeneratePng(string content, int pixelsPerModule = 6);
}
