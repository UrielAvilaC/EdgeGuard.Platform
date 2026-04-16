using Microsoft.AspNetCore.DataProtection;

namespace Dicom.Edge.Security.Cryptography;

/// <summary>
/// Implements <see cref="ISettingEncryptionService"/> using ASP.NET Core Data Protection.
/// Provides automatic key rotation, no manual key/IV management, and tamper-proof ciphertext.
/// </summary>
public sealed class DataProtectionSettingEncryptionService : ISettingEncryptionService
{
    private const string EncryptedPrefix = "ENC:";
    private const string Purpose = "EdgeGuard.SystemSettings.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSettingEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        var cipher = _protector.Protect(plainText);
        return $"{EncryptedPrefix}{cipher}";
    }

    public string Decrypt(string cipherText)
    {
        if (!IsEncrypted(cipherText))
            return cipherText;

        var payload = cipherText[EncryptedPrefix.Length..];
        return _protector.Unprotect(payload);
    }

    public bool IsEncrypted(string value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
}
