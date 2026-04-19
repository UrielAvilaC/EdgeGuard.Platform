namespace Dicom.Edge.Hub.Application.Identity;

/// <summary>
/// Validates password complexity requirements.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>
    /// Validates password complexity. Returns null if valid, or an error message.
    /// </summary>
    public static string? Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password is required.";

        if (password.Length < MinLength)
            return $"Password must be at least {MinLength} characters.";

        if (password.Length > MaxLength)
            return $"Password must not exceed {MaxLength} characters.";

        if (!password.Any(char.IsUpper))
            return "Password must contain at least one uppercase letter.";

        if (!password.Any(char.IsLower))
            return "Password must contain at least one lowercase letter.";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit.";

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return "Password must contain at least one special character.";

        return null;
    }
}
