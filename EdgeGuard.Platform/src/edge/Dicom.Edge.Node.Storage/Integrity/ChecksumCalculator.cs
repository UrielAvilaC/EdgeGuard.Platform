using System.Security.Cryptography;

namespace Dicom.Edge.Node.Storage.Integrity;

/// <summary>
/// Computes SHA-256 checksums for DICOM files.
/// Used for integrity verification after writes, archive validation,
/// and duplicate detection.
/// </summary>
internal static class ChecksumCalculator
{
    /// <summary>
    /// Computes the SHA-256 hash of a file and returns it as a lowercase hex string.
    /// Uses async streaming to avoid loading the entire file into memory.
    /// </summary>
    public static async Task<string> ComputeSha256Async(
        string filePath, CancellationToken ct = default)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// Computes the SHA-256 hash from a byte span (for in-memory verification).
    /// </summary>
    public static string ComputeSha256(ReadOnlySpan<byte> data)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(data, hash);
        return Convert.ToHexStringLower(hash);
    }
}
