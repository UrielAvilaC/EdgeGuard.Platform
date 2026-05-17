using System.Linq.Expressions;

namespace Dicom.Edge.Common.Sorting;

/// <summary>
/// Generic IQueryable sorting helper that applies dynamic column sorting
/// while validating allowed field names to prevent injection.
/// </summary>
public static class SortingExtensions
{
    /// <summary>
    /// Applies dynamic sorting to an <see cref="IQueryable{T}"/> based on a field name and direction.
    /// Falls back to <paramref name="defaultSort"/> if the field is null or not in the allowed map.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="query">The queryable to sort.</param>
    /// <param name="sortBy">Column name (case-insensitive). Must match a key in <paramref name="allowedSorts"/>.</param>
    /// <param name="sortDir">"asc" or "desc". Defaults to "asc" if unrecognized.</param>
    /// <param name="allowedSorts">
    /// Dictionary mapping lowercase field names to lambda expressions.
    /// Example: <c>{ ["name"] = q => q.Name, ["createdAt"] = q => q.CreatedAt }</c>
    /// </param>
    /// <param name="defaultSort">
    /// Fallback ordering applied when <paramref name="sortBy"/> is null or not in the allowed map.
    /// </param>
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        string? sortDir,
        Dictionary<string, Expression<Func<T, object?>>> allowedSorts,
        Func<IQueryable<T>, IOrderedQueryable<T>> defaultSort)
    {
        var isDescending = string.Equals(sortDir?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(sortBy) &&
            allowedSorts.TryGetValue(sortBy.Trim().ToLowerInvariant(), out var keySelector))
        {
            return isDescending
                ? query.OrderByDescending(keySelector)
                : query.OrderBy(keySelector);
        }

        return defaultSort(query);
    }
}
