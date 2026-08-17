using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Dicom.Edge.Security.Authentication
{
    /// <summary>
    /// Enterprise-grade JWT token service with comprehensive security features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Features:
    /// <list type="bullet">
    ///   <item><description>Secure token generation with configurable expiration</description></item>
    ///   <item><description>Complete token validation (signature, issuer, audience, lifetime)</description></item>
    ///   <item><description>Refresh token support for long-lived sessions</description></item>
    ///   <item><description>Token revocation capability</description></item>
    ///   <item><description>Multiple roles and custom claims support</description></item>
    ///   <item><description>Comprehensive audit logging</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class JwtTokenService : ITokenService
    {
        private readonly JwtTokenServiceOptions _options;
        private readonly ILogger<JwtTokenService> _logger;
        private readonly byte[] _key;

        // In-memory revoked tokens (use Redis or database in production)
        private static readonly HashSet<string> _revokedTokens = new();
        private static readonly object _revocationLock = new();

        public JwtTokenService(
            IOptions<JwtTokenServiceOptions> options,
            ILogger<JwtTokenService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateConfiguration();
            _key = Encoding.UTF8.GetBytes(_options.SecretKey);
        }

        private void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_options.SecretKey))
                throw new InvalidOperationException("JWT SecretKey cannot be empty");

            if (_options.SecretKey.Length < 32)
                throw new InvalidOperationException("JWT SecretKey must be at least 32 characters for security");

            if (string.IsNullOrWhiteSpace(_options.Issuer))
                throw new InvalidOperationException("JWT Issuer must be configured");

            if (string.IsNullOrWhiteSpace(_options.Audience))
                throw new InvalidOperationException("JWT Audience must be configured");

            _logger.LogInformation(
                "JwtTokenService initialized with Issuer={Issuer}, Audience={Audience}, TokenExpiration={Expiration}",
                _options.Issuer,
                _options.Audience,
                _options.AccessTokenExpiration);
        }

        /// <summary>
        /// Generates access and refresh tokens for the specified request.
        /// </summary>
        public TokenResponse GenerateToken(TokenRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required", nameof(request));

            var claims = BuildClaims(request);
            var accessToken = GenerateAccessToken(claims);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation(
                "Generated token for user {UserId} with roles {Roles}",
                request.UserId,
                request.Roles != null ? string.Join(", ", request.Roles) : "none");

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = (int)_options.AccessTokenExpiration.TotalSeconds,
                TokenType = "Bearer",
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_options.AccessTokenExpiration)
            };
        }

        /// <summary>
        /// Validates a JWT token and returns the result with claims principal.
        /// </summary>
        public TokenValidationResult ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return TokenValidationResult.Failed("Token is empty");
            }

            // Check if token is revoked
            lock (_revocationLock)
            {
                if (_revokedTokens.Contains(token))
                {
                    _logger.LogWarning("Attempt to use revoked token");
                    return TokenValidationResult.Failed("Token has been revoked");
                }
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // Additional security checks
                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature, StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogWarning("Invalid token algorithm detected");
                    return TokenValidationResult.Failed("Invalid token algorithm");
                }

                return TokenValidationResult.Success(principal, jwtToken.ValidTo);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogDebug("Token expired: {Message}", ex.Message);
                return TokenValidationResult.Failed("Token has expired", isExpired: true);
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("Invalid token signature: {Message}", ex.Message);
                return TokenValidationResult.Failed("Invalid token signature");
            }
            catch (SecurityTokenInvalidIssuerException ex)
            {
                _logger.LogWarning("Invalid token issuer: {Message}", ex.Message);
                return TokenValidationResult.Failed("Invalid token issuer");
            }
            catch (SecurityTokenInvalidAudienceException ex)
            {
                _logger.LogWarning("Invalid token audience: {Message}", ex.Message);
                return TokenValidationResult.Failed("Invalid token audience");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token validation failed");
                return TokenValidationResult.Failed($"Token validation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Revokes a token, preventing its future use.
        /// </summary>
        public void RevokeToken(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                lock (_revocationLock)
                {
                    _revokedTokens.Add(token);
                }
                _logger.LogInformation("Token revoked");
            }
        }

        /// <summary>
        /// Extracts claims principal from an expired token (for refresh scenarios).
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = GetValidationParameters();
                validationParameters.ValidateLifetime = false; // Don't validate expiration

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract principal from expired token");
                return null;
            }
        }

        /// <summary>
        /// Checks if a token is revoked.
        /// </summary>
        public bool IsTokenRevoked(string token)
        {
            lock (_revocationLock)
            {
                return _revokedTokens.Contains(token);
            }
        }

        private List<Claim> BuildClaims(TokenRequest request)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, request.UserId),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                new("userId", request.UserId),
            };

            // User name
            if (!string.IsNullOrWhiteSpace(request.UserName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.Name, request.UserName));
                claims.Add(new Claim("userName", request.UserName));
            }

            // Multiple roles support
            if (request.Roles != null)
            {
                foreach (var role in request.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            // Edge Node ID
            if (request.EdgeNodeId != null)
                claims.Add(new Claim("edgeNodeId", request.EdgeNodeId));

            // Facility ID
            if (request.FacilityId != null)
                claims.Add(new Claim("facilityId", request.FacilityId));

            // Department
            if (request.Department != null)
                claims.Add(new Claim("department", request.Department));

            // Custom claims from request
            if (request.CustomClaims != null)
            {
                foreach (var (key, value) in request.CustomClaims)
                {
                    claims.Add(new Claim(key, value));
                }
            }

            return claims;
        }

        private string GenerateAccessToken(List<Claim> claims)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(_options.AccessTokenExpiration),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(_key),
                    SecurityAlgorithms.HmacSha256Signature),
                NotBefore = DateTime.UtcNow
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private TokenValidationParameters GetValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_key),
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateLifetime = _options.ValidateLifetime,
                ClockSkew = _options.ClockSkew,
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };
        }

        /// <summary>
        /// Clears all revoked tokens (maintenance operation).
        /// </summary>
        public void ClearRevokedTokens()
        {
            lock (_revocationLock)
            {
                var count = _revokedTokens.Count;
                _revokedTokens.Clear();
                _logger.LogInformation("Cleared {Count} revoked tokens", count);
            }
        }
    }
}
