namespace Dicom.Edge.Hub.Domain.Aggregates.Modalities;

public interface IModalityRepository
{
    /// <summary>Returns the catalog ordered by <c>SortOrder</c>; optionally only assignable codes.</summary>
    Task<IReadOnlyList<Modality>> GetAllAsync(bool supportedOnly = false, CancellationToken ct = default);

    Task<Modality?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Returns the catalog entries matching the given codes (case-insensitive).</summary>
    Task<IReadOnlyList<Modality>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken ct = default);
}
