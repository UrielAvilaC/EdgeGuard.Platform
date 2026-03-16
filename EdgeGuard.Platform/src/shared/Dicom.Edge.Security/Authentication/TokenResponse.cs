namespace Dicom.Edge.Security.Authentication
{
    /// <summary>
    /// Response object containing generated tokens.
    /// </summary>
    public class TokenResponse
    {
        /// <summary>
        /// JWT access token for authentication.
        /// </summary>
        public string AccessToken { get; set; } = default!;
        
        /// <summary>
        /// Refresh token for obtaining new access tokens.
        /// </summary>
        public string RefreshToken { get; set; } = default!;
        
        /// <summary>
        /// Access token expiration time in seconds.
        /// </summary>
        public int ExpiresIn { get; set; }
        
        /// <summary>
        /// Token type (typically "Bearer").
        /// </summary>
        public string TokenType { get; set; } = "Bearer";
        
        /// <summary>
        /// UTC timestamp when the token was issued.
        /// </summary>
        public DateTime IssuedAt { get; set; }
        
        /// <summary>
        /// UTC timestamp when the token expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Scope of access granted by this token.
        /// </summary>
        public string? Scope { get; set; }
    }
}
