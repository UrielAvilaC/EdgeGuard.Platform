using System.Security.Claims;

namespace Dicom.Edge.Security.Authentication
{
    /// <summary>
    /// Service for JWT token generation and validation.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates access and refresh tokens for a user.
        /// </summary>
        /// <param name="request">Token generation request with user details.</param>
        /// <returns>Token response containing access and refresh tokens.</returns>
        TokenResponse GenerateToken(TokenRequest request);

        /// <summary>
        /// Validates a JWT token and returns validation result.
        /// </summary>
        /// <param name="token">JWT token to validate.</param>
        /// <returns>Validation result with claims principal if valid.</returns>
        TokenValidationResult ValidateToken(string token);

        /// <summary>
        /// Revokes a token, preventing its future use.
        /// </summary>
        /// <param name="token">Token to revoke.</param>
        void RevokeToken(string token);

        /// <summary>
        /// Extracts claims principal from an expired token (for refresh scenarios).
        /// </summary>
        /// <param name="token">Expired token.</param>
        /// <returns>Claims principal if extraction succeeds; otherwise null.</returns>
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

        /// <summary>
        /// Checks if a token has been revoked.
        /// </summary>
        /// <param name="token">Token to check.</param>
        /// <returns>True if revoked; otherwise false.</returns>
        bool IsTokenRevoked(string token);

        // Legacy methods for backward compatibility
        [Obsolete("Use GenerateToken(TokenRequest) instead")]
        string GenerateToken(string userId, string role);
    }
}
