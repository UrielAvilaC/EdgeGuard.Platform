using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.Configuration
{
    /// <summary>
    /// Data transfer object for modality configuration.
    /// </summary>
    public class ModalityConfigurationDto
    {
        public string? Id { get; set; }
        
        [Required]
        [StringLength(16, MinimumLength = 1)]
        [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "AE Title must contain only uppercase letters, numbers, and underscores")]
        public string AETitle { get; set; } = default!;
        
        [StringLength(100)]
        public string? DisplayName { get; set; }
        
        [Required]
        [RegularExpression(@"^(\d{1,3}\.){3}\d{1,3}$|^([0-9a-fA-F]{0,4}:){1,7}[0-9a-fA-F]{0,4}$", 
            ErrorMessage = "Must be a valid IPv4 or IPv6 address")]
        public string IpAddress { get; set; } = default!;
        
        [Range(1, 65535)]
        public int Port { get; set; } = 104;
        
        public bool IsEnabled { get; set; } = true;
        public bool RequiresAuth { get; set; } = true;
        
        [StringLength(16)]
        public string? DefaultDestination { get; set; }
        
        [Range(0, 100)]
        public int MaxConcurrentConnections { get; set; } = 5;
        
        public int TimeoutMinutes { get; set; } = 5;
        
        [StringLength(100)]
        public string? Manufacturer { get; set; }
        
        [StringLength(100)]
        public string? ModelName { get; set; }
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        [StringLength(100)]
        public string? Department { get; set; }
        
        public DateTime? LastConnectionAt { get; set; }
        public bool IsOnline { get; set; }
    }
}
