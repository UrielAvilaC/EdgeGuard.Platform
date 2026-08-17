namespace Dicom.Edge.Hub.Domain.Aggregates.Equipment;

/// <summary>
/// Join row linking a <see cref="NodeEquipment"/> to a modality catalog code (M:N).
/// Composite key (<see cref="EquipmentId"/>, <see cref="ModalityCode"/>).
/// </summary>
public sealed class EquipmentModality
{
    public string EquipmentId  { get; private set; } = default!;
    public string ModalityCode { get; private set; } = default!;

    private EquipmentModality() { }

    public EquipmentModality(string equipmentId, string modalityCode)
    {
        EquipmentId  = equipmentId;
        ModalityCode = modalityCode;
    }
}
