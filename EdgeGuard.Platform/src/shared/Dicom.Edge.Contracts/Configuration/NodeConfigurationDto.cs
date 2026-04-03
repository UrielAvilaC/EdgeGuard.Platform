namespace Dicom.Edge.Contracts.Configuration;

/// <summary>
/// Payload the Hub sends to an Edge Node to apply a configuration update.
/// Only non-null fields are applied; null fields are left unchanged on the node.
/// </summary>
public sealed class NodeConfigurationDto
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public string? NodeName     { get; set; }
    public string? NodeAeTitle  { get; set; }
    public string? Description  { get; set; }

    // ── DICOM ─────────────────────────────────────────────────────────────────
    public bool?         ValidateCallingAe          { get; set; }
    public List<string>? AllowedAeTitles            { get; set; }
    public int?          MaxAssociations            { get; set; }
    public int?          DicomPort                  { get; set; }
    public int?          StudyCompletionTimeoutSec  { get; set; }

    // ── Transfer ──────────────────────────────────────────────────────────────
    public int? MaxRetries           { get; set; }
    public int? RetryBaseDelaySec    { get; set; }
    public int? TransferTimeoutSec   { get; set; }
    public int? MaxConcurrentTransfers { get; set; }

    // ── Cleanup ───────────────────────────────────────────────────────────────
    public bool? CleanupEnabled    { get; set; }
    public int?  RetainDays        { get; set; }
    public int?  RetainSentDays    { get; set; }
    public int?  MaxStorageGb      { get; set; }

    // ── Modality & routing (full replace — null = no change) ──────────────────
    public List<ModalityConfigurationDto>? Modalities   { get; set; }
    public List<RoutingRuleDto>?           RoutingRules { get; set; }

    // ── Metadata ──────────────────────────────────────────────────────────────
    public string   ConfigVersion { get; set; } = default!;
    public DateTime IssuedAt      { get; set; }
}
