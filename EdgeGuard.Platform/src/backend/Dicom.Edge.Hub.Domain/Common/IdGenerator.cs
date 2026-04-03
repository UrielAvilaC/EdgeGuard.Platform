namespace Dicom.Edge.Hub.Domain.Common;

/// <summary>
/// Generates sortable, unique string identifiers based on timestamp + random suffix.
/// </summary>
public static class IdGenerator
{
    /// <summary>
    /// Generates a new sortable unique ID (timestamp-based prefix for ordering).
    /// </summary>
    public static string NewId()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var random = Guid.NewGuid().ToString("N")[..12];
        return $"{timestamp}-{random}";
    }
}
