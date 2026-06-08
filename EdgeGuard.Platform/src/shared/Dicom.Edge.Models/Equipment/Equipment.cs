namespace Dicom.Edge.Models.Equipment
{
    /// <summary>
    /// Edge-local copy of a Hub-managed equipment (modality device) record, kept in sync
    /// via the Hub push (<c>POST /api/equipment/sync</c>). Drives association acceptance
    /// (by <see cref="AeTitle"/>) and per-equipment MWL filtering (by its assigned
    /// <see cref="Modalities"/> and optional <see cref="StationAeTitle"/>).
    /// </summary>
    public class Equipment
    {
        public string  Id             { get; set; } = default!;
        public string  AeTitle        { get; set; } = default!;
        public string? DisplayName    { get; set; }
        public string? StationAeTitle { get; set; }
        public string? StationName    { get; set; }
        public string? IpAddress      { get; set; }
        public bool    IsEnabled      { get; set; } = true;

        public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>Assigned modality codes (join rows).</summary>
        public List<EquipmentModality> Modalities { get; set; } = [];
    }

    /// <summary>Join row linking an <see cref="Equipment"/> to a modality code.</summary>
    public class EquipmentModality
    {
        public string EquipmentId  { get; set; } = default!;
        public string ModalityCode { get; set; } = default!;
    }

    /// <summary>
    /// Edge-local copy of the modality reference catalog, seeded from the shared
    /// <c>ModalitySeed</c>. Provided for self-contained display/validation on the node.
    /// </summary>
    public class ModalityCatalogEntry
    {
        public string Code        { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public bool   IsSupported { get; set; }
        public bool   IsActive    { get; set; } = true;
        public int    SortOrder   { get; set; }
    }
}
