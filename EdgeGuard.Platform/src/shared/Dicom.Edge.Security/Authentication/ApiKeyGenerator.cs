using System.Security.Cryptography;

namespace Dicom.Edge.Security.Authentication;

/// <summary>
/// Generates cryptographically secure API keys with a recognizable prefix.
/// Format: "edg_{base62(32 bytes)}" — e.g. "edg_k1Abc2Def3Ghi4Jkl5Mno6Pqr7Stu8"
/// </summary>
public static class ApiKeyGenerator
{
    private const string Prefix = "edg_";
    private const int KeyBytes = 32;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    /// <summary>
    /// Generates a new API key with the "edg_" prefix.
    /// </summary>
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyBytes);
        var chars = new char[KeyBytes];
        for (var i = 0; i < KeyBytes; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];

        return string.Concat(Prefix, new string(chars));
    }
}
