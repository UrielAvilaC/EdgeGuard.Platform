namespace Dicom.Edge.Security.Cryptography;

/// <summary>
/// Encrypts and decrypts system setting values using ASP.NET Core Data Protection.
/// Used for sensitive configuration such as messaging provider credentials.
/// </summary>
public interface ISettingEncryptionService
{
    /// <summary>
    /// Encrypts a plain-text value. Returns a string prefixed with <c>ENC:</c>.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts a previously encrypted value. Strips the <c>ENC:</c> prefix before decryption.
    /// Returns the original plain text if the value is not encrypted.
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Determines whether a stored value is encrypted (starts with <c>ENC:</c>).
    /// </summary>
    bool IsEncrypted(string value);
}
