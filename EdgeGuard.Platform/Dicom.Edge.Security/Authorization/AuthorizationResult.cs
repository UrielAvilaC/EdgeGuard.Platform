namespace Dicom.Edge.Security.Authorization
{
    /// <summary>
    /// Result of an authorization check.
    /// </summary>
    public class AuthorizationResult
    {
        /// <summary>
        /// Whether authorization succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }
        
        /// <summary>
        /// Reason for authorization failure.
        /// </summary>
        public string? FailureReason { get; private set; }
        
        /// <summary>
        /// Additional context about the authorization decision.
        /// </summary>
        public Dictionary<string, object>? Context { get; private set; }

        /// <summary>
        /// Creates a successful authorization result.
        /// </summary>
        public static AuthorizationResult Success() => 
            new() { Succeeded = true };

        /// <summary>
        /// Creates a successful authorization result with context.
        /// </summary>
        public static AuthorizationResult Success(Dictionary<string, object> context) =>
            new() { Succeeded = true, Context = context };

        /// <summary>
        /// Creates a failed authorization result.
        /// </summary>
        public static AuthorizationResult Failed(string reason) =>
            new() { Succeeded = false, FailureReason = reason };

        /// <summary>
        /// Creates a failed authorization result with context.
        /// </summary>
        public static AuthorizationResult Failed(string reason, Dictionary<string, object> context) =>
            new() { Succeeded = false, FailureReason = reason, Context = context };
    }
}
