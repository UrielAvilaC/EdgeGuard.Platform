namespace Dicom.Edge.Security.Authentication
{
    /// <summary>
    /// Request object for token generation.
    /// </summary>
    public class TokenRequest
    {
        /// <summary>
        /// User identifier (required).
        /// </summary>
        public string UserId { get; set; } = default!;
        
        /// <summary>
        /// User display name.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// List of roles assigned to the user.
        /// </summary>
        public List<string>? Roles { get; set; }
        
        /// <summary>
        /// Edge Node ID if user is associated with a specific node.
        /// </summary>
        public string? EdgeNodeId { get; set; }
        
        /// <summary>
        /// Facility/Organization ID.
        /// </summary>
        public string? FacilityId { get; set; }
        
        /// <summary>
        /// Department within the facility.
        /// </summary>
        public string? Department { get; set; }
        
        /// <summary>
        /// Additional custom claims to include in the token.
        /// </summary>
        public Dictionary<string, string>? CustomClaims { get; set; }
    }
}
