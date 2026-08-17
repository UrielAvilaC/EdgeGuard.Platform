using System.ComponentModel.DataAnnotations;

namespace Dicom.Edge.Contracts.WhatsApp;

// ── Templates ────────────────────────────────────────────────────────────────

public sealed record WhatsAppTemplateDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string ContentSid { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<WhatsAppTemplateVariableDto> Variables { get; init; } = [];
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record WhatsAppTemplateVariableDto
{
    public int Position { get; init; }
    public required string Tag { get; init; }
}

public sealed class CreateWhatsAppTemplateRequest
{
    [Required, StringLength(128, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string ContentSid { get; init; }

    [StringLength(512)]
    public string? Description { get; init; }

    /// <summary>Ordered variable tags; index 0 → position 1. May be empty (template with no variables).</summary>
    public string[] Tags { get; init; } = [];
}

public sealed class UpdateWhatsAppTemplateRequest
{
    [Required, StringLength(128, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(64, MinimumLength = 1)]
    public required string ContentSid { get; init; }

    [StringLength(512)]
    public string? Description { get; init; }

    /// <summary>Ordered variable tags; index 0 → position 1. May be empty (template with no variables).</summary>
    public string[] Tags { get; init; } = [];
}

// ── Auto-Send Rules ──────────────────────────────────────────────────────────

public sealed record WhatsAppAutoSendRuleDto
{
    public required string Id { get; init; }
    public required string StudyStatus { get; init; }
    public required string TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public bool IsEnabled { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CreateWhatsAppAutoSendRuleRequest
{
    [Required, StringLength(32, MinimumLength = 1)]
    public required string StudyStatus { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    public required string TemplateId { get; init; }

    [StringLength(512)]
    public string? Description { get; init; }
}

public sealed class UpdateWhatsAppAutoSendRuleRequest
{
    [StringLength(50)]
    public string? TemplateId { get; init; }

    public bool? IsEnabled { get; init; }

    [StringLength(512)]
    public string? Description { get; init; }
}

// ── Notifications ────────────────────────────────────────────────────────────

public sealed record WhatsAppNotificationDto
{
    public required string Id { get; init; }
    public required string StudyId { get; init; }
    public string? PatientId { get; init; }
    public required string PhoneNumber { get; init; }
    public string? NormalizedPhone { get; init; }
    public string? TemplateId { get; init; }
    public string? ContentSid { get; init; }
    public string? StudyStatus { get; init; }
    public required string Status { get; init; }
    public required string TriggeredBy { get; init; }
    public string? ProviderName { get; init; }
    public string? ProviderMessageId { get; init; }
    public DateTime? SentAt { get; init; }
    public int Attempts { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ── Manual Send ──────────────────────────────────────────────────────────────

public sealed class SendWhatsAppManualRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public required string StudyId { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    public required string TemplateId { get; init; }

    [Required, MinLength(1)]
    public required WhatsAppRecipientDto[] Recipients { get; init; }
}

public sealed class WhatsAppRecipientDto
{
    [Required, StringLength(32, MinimumLength = 5)]
    public required string PhoneNumber { get; init; }

    [StringLength(256)]
    public string? Name { get; init; }
}

public sealed record SendWhatsAppManualResponse
{
    public IReadOnlyList<string> NotificationIds { get; init; } = [];
    public int Succeeded { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<WhatsAppSendResultDto> Results { get; init; } = [];
}

public sealed record WhatsAppSendResultDto
{
    public required string PhoneNumber { get; init; }
    public string? NormalizedPhone { get; init; }
    public string? NotificationId { get; init; }
    public bool Success { get; init; }
    public string? Error { get; init; }
}

// ── Tags & Config ────────────────────────────────────────────────────────────

public sealed record WhatsAppTemplateTagDto
{
    public required string Tag { get; init; }
    public required string Description { get; init; }
    public string? Example { get; init; }
}

public sealed record WhatsAppConfigStatusDto
{
    public bool Enabled { get; init; }
    public bool AutomaticDeliveryEnabled { get; init; }
    public required string Provider { get; init; }
    public bool ProviderConfigured { get; init; }
    public required string DefaultCountryPrefix { get; init; }
    public int ActiveTemplatesCount { get; init; }
    public int ActiveRulesCount { get; init; }
    public int PendingNotifications { get; init; }
    public int FailedNotifications { get; init; }
}
