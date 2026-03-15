using System.Security.Claims;

namespace Dicom.Edge.Security.Authentication
{
    /// <summary>
    /// Result of token validation operation.
    /// </summary>
    public class TokenValidationResult
    {
        /// <summary>
        /// Whether the token is valid.
        /// </summary>
        public bool IsValid { get; private set; }
        
        /// <summary>
        /// Error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; private set; }
        
        /// <summary>
        /// Claims principal extracted from valid token.
        /// </summary>
        public ClaimsPrincipal? Principal { get; private set; }
        
        /// <summary>
        /// Token expiration timestamp.
        /// </summary>
        public DateTime? ExpiresAt { get; private set; }
        
        /// <summary>
        /// Whether the token is expired.
        /// </summary>
        public bool IsExpired { get; private set; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static TokenValidationResult Success(ClaimsPrincipal principal, DateTime? expiresAt = null) =>
            new() { IsValid = true, Principal = principal, ExpiresAt = expiresAt };

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        public static TokenValidationResult Failed(string error, bool isExpired = false) =>
            new() { IsValid = false, ErrorMessage = error, IsExpired = isExpired };
    }
}
