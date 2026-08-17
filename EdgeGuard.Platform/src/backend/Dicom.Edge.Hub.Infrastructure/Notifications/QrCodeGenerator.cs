using Dicom.Edge.Hub.Application.Notifications;
using QRCoder;

namespace Dicom.Edge.Hub.Infrastructure.Notifications;

/// <summary>
/// QRCoder-based <see cref="IQrCodeGenerator"/>. Uses <see cref="PngByteQRCode"/> so it
/// works cross-platform without System.Drawing.
/// </summary>
public sealed class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string content, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
