using System.Text.RegularExpressions;

namespace Dicom.Edge.Hub.Domain.ValueObjects;

/// <summary>
/// Normalizes and validates phone numbers for WhatsApp messaging.
/// Strips non-digit characters, applies a default country prefix for 10-digit numbers,
/// and validates the result has between 10 and 15 digits.
/// </summary>
public static partial class PhoneNumberNormalizer
{
    /// <summary>
    /// Normalizes a raw phone number. Returns <c>null</c> if invalid.
    /// </summary>
    /// <param name="raw">Raw phone input (may include spaces, dashes, parentheses, +).</param>
    /// <param name="defaultCountryPrefix">
    /// Prefix to prepend when the number has exactly 10 digits (e.g. "+521" for Mexico mobile).
    /// </param>
    public static string? Normalize(string? raw, string defaultCountryPrefix = "+521")
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Strip everything except digits and leading +
        var hasPlus = raw.TrimStart().StartsWith('+');
        var digits = DigitsOnly().Replace(raw, "");

        if (digits.Length < 10 || digits.Length > 15)
            return null;

        // 10 digits → local number, prepend default country prefix
        if (digits.Length == 10)
            return $"{defaultCountryPrefix}{digits}";

        // Already has country code — ensure + prefix
        return hasPlus ? $"+{digits}" : $"+{digits}";
    }

    /// <summary>
    /// Returns <c>true</c> if the raw value can be normalized to a valid phone number.
    /// </summary>
    public static bool IsValid(string? raw, string defaultCountryPrefix = "+521") =>
        Normalize(raw, defaultCountryPrefix) is not null;

    /// <summary>
    /// Redacts a normalized phone number for audit logging, showing only the last 4 digits.
    /// </summary>
    /// <example>"+5215512345678" → "+521****5678"</example>
    public static string RedactForAudit(string? normalizedPhone)
    {
        if (string.IsNullOrWhiteSpace(normalizedPhone) || normalizedPhone.Length < 8)
            return "****";

        var visible = normalizedPhone[..4];
        var last4 = normalizedPhone[^4..];
        return $"{visible}****{last4}";
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
