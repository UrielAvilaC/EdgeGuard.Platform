using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.Configuration
{
    /// <summary>
    /// Data transfer object for routing rule configuration.
    /// </summary>
    public class RoutingRuleDto
    {
        public string? Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = default!;
        
        [Range(0, 1000)]
        public int Priority { get; set; }
        
        public bool IsEnabled { get; set; } = true;
        
        // Conditions
        [StringLength(16)]
        public string? SourceAeTitle { get; set; }
        
        [StringLength(10)]
        public string? Modality { get; set; }
        
        [StringLength(200)]
        public string? StudyDescriptionContains { get; set; }
        
        [StringLength(50)]
        public string? InstitutionName { get; set; }
        
        public int? MinInstanceCount { get; set; }
        public int? MaxInstanceCount { get; set; }
        
        // Actions
        [Required]
        [StringLength(16)]
        public string DestinationAeTitle { get; set; } = default!;
        
        public bool SendToHub { get; set; } = true;
        public bool SendToPacs { get; set; }
        public bool ArchiveImmediately { get; set; }
        public bool AnonymizeBeforeSending { get; set; }
        
        // Metadata
        public int MatchCount { get; set; }
        public DateTime? LastMatchedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = default!;
    }
}
