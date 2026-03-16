namespace Dicom.Edge.Security.Authentication
{
    /// <summary>
    /// Configuration options for JWT token generation and validation.
    /// </summary>
    public class JwtTokenServiceOptions
    {
        /// <summary>
        /// Secret key for signing tokens (minimum 32 characters recommended).
        /// </summary>
        public string SecretKey { get; set; } = default!;
        
        /// <summary>
        /// Token issuer (typically your API URL).
        /// </summary>
        public string Issuer { get; set; } = "EdgeGuard.Platform";
        
        /// <summary>
        /// Token audience (typically your client app).
        /// </summary>
        public string Audience { get; set; } = "EdgeGuard.Clients";
        
        /// <summary>
        /// Access token expiration time.
        /// </summary>
        public TimeSpan AccessTokenExpiration { get; set; } = TimeSpan.FromMinutes(15);
        
        /// <summary>
        /// Refresh token expiration time.
        /// </summary>
        public TimeSpan RefreshTokenExpiration { get; set; } = TimeSpan.FromDays(7);
        
        /// <summary>
        /// Clock skew for token validation (to handle clock differences).
        /// </summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Whether to validate token lifetime.
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;
        
        /// <summary>
        /// Whether to require HTTPS for token operations.
        /// </summary>
        public bool RequireHttpsMetadata { get; set; } = true;
    }
}
