namespace Dicom.Edge.Common.Pagination;

/// <summary>
/// Generic API response wrapper for paged results.
/// Produced by <c>PagedResult&lt;T&gt;.ToResponse()</c> extension in mapping profiles.
/// </summary>
public sealed class PagedResponse<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public IReadOnlyList<T> Items { get; init; } = [];
}

public static class PagedResultExtensions
{
    /// <summary>
    /// Projects a <see cref="PagedResult{TEntity}"/> into a <see cref="PagedResponse{TDto}"/>
    /// using the supplied mapping function.
    /// </summary>
    public static PagedResponse<TDto> ToPagedResponse<TEntity, TDto>(
        this PagedResult<TEntity> source,
        Func<TEntity, TDto> mapper)
    {
        return new PagedResponse<TDto>
        {
            Page = source.Page,
            PageSize = source.PageSize,
            TotalCount = source.TotalCount,
            TotalPages = source.TotalPages,
            Items = source.Items.Select(mapper).ToList()
        };
    }
}
