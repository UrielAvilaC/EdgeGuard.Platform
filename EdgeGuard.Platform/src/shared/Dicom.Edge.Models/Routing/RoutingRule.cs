namespace Dicom.Edge.Models.Routing
{
    /// <summary>
    /// Represents a configurable routing rule for directing studies to specific destinations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Routing rules enable dynamic, condition-based routing of DICOM studies.
    /// Rules are evaluated in priority order (lower number = higher priority)
    /// until a matching rule is found.
    /// </para>
    /// <para>
    /// <strong>Rule Evaluation:</strong>
    /// <code>
    /// foreach (var rule in rules.OrderBy(r => r.Priority))
    /// {
    ///     if (rule.IsEnabled &amp;&amp; rule.Matches(study))
    ///     {
    ///         return rule.DestinationAeTitle;
    ///     }
    /// }
    /// return defaultDestination;
    /// </code>
    /// </para>
    /// </remarks>
    public class RoutingRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// User-friendly name for the rule.
        /// </summary>
        public string Name { get; set; } = default!;
        
        /// <summary>
        /// Evaluation priority (lower = higher priority, 0 = highest).
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Whether this rule is active.
        /// </summary>
        public bool IsEnabled { get; set; } = true;
        
        // ==================== Conditions ====================
        
        /// <summary>
        /// Match studies from this source AE Title (null = any).
        /// </summary>
        public string? SourceAeTitle { get; set; }
        
        /// <summary>
        /// Match studies with this modality (null = any).
        /// Examples: "CT", "MR", "CR", "US"
        /// </summary>
        public string? Modality { get; set; }
        
        /// <summary>
        /// Match studies containing this text in Study Description (case-insensitive, null = any).
        /// </summary>
        public string? StudyDescriptionContains { get; set; }
        
        /// <summary>
        /// Match studies with this Accession Number (null = any).
        /// </summary>
        public string? AccessionNumber { get; set; }
        
        /// <summary>
        /// Match studies from this institution (null = any).
        /// </summary>
        public string? InstitutionName { get; set; }
        
        /// <summary>
        /// Match studies from this department (null = any).
        /// </summary>
        public string? Department { get; set; }
        
        /// <summary>
        /// Match studies with instance count greater than or equal to this value (null = any).
        /// </summary>
        /// <remarks>
        /// Example: Route large studies (&gt; 1000 images) to high-capacity storage.
        /// </remarks>
        public int? MinInstanceCount { get; set; }
        
        /// <summary>
        /// Match studies with instance count less than or equal to this value (null = any).
        /// </summary>
        public int? MaxInstanceCount { get; set; }
        
        /// <summary>
        /// Custom JSON condition for advanced scenarios.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Store complex conditions as JSON for flexibility:
        /// <code>
        /// {
        ///   "StudyTime": { "Start": "18:00", "End": "08:00" },  // After-hours
        ///   "PatientAge": { "Min": 0, "Max": 18 },               // Pediatric
        ///   "BodyPart": ["CHEST", "ABDOMEN"]                     // Specific anatomy
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        public string? CustomConditionJson { get; set; }
        
        // ==================== Actions ====================
        
        /// <summary>
        /// Destination AE Title to route matching studies to.
        /// </summary>
        public string DestinationAeTitle { get; set; } = default!;
        
        /// <summary>
        /// Whether to also send to the central Hub.
        /// </summary>
        public bool SendToHub { get; set; } = true;
        
        /// <summary>
        /// Whether to also send to configured PACS.
        /// </summary>
        public bool SendToPacs { get; set; }
        
        /// <summary>
        /// Whether to archive immediately after routing (vs. waiting for retention policy).
        /// </summary>
        public bool ArchiveImmediately { get; set; }
        
        /// <summary>
        /// Whether to anonymize PHI before sending.
        /// </summary>
        public bool AnonymizeBeforeSending { get; set; }
        
        // ==================== Metadata ====================
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = default!;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        
        /// <summary>
        /// Number of studies matched by this rule (for analytics).
        /// </summary>
        public int MatchCount { get; set; }
        
        /// <summary>
        /// UTC timestamp of last match.
        /// </summary>
        public DateTime? LastMatchedAt { get; set; }
    }
}
