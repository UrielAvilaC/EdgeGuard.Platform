using System.Linq.Expressions;
using Dicom.Edge.Common.Pagination;

namespace Dicom.Edge.Abstractions.Persistence
{
    /// <summary>
    /// Generic repository interface for data access operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository.</typeparam>
    /// <remarks>
    /// <para>
    /// Provides CRUD operations and querying capabilities following the Repository pattern.
    /// Implementations should be unit-of-work aware and coordinate with <see cref="IUnitOfWork"/>.
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var repository = serviceProvider.GetRequiredService&lt;IRepository&lt;DicomStudy&gt;&gt;();
    /// var study = await repository.GetByIdAsync(studyUid);
    /// study.Status = StudyStatus.Completed;
    /// await repository.UpdateAsync(study);
    /// await unitOfWork.SaveChangesAsync();
    /// </code>
    /// </para>
    /// </remarks>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Retrieves an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The entity if found; otherwise, null.</returns>
        Task<TEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves all entities.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A read-only list of all entities.</returns>
        /// <remarks>
        /// Use with caution on large tables. Consider pagination instead.
        /// </remarks>
        Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds entities matching a predicate.
        /// </summary>
        /// <param name="predicate">Filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching entities.</returns>
        Task<IReadOnlyList<TEntity>> FindAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds the first entity matching a predicate.
        /// </summary>
        /// <param name="predicate">Filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first matching entity, or null.</returns>
        Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new entity to the repository.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The added entity.</returns>
        /// <remarks>
        /// Changes are not persisted until SaveChangesAsync is called on the UnitOfWork.
        /// </remarks>
        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds multiple entities in a single operation.
        /// </summary>
        /// <param name="entities">The entities to add.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Changes are not persisted until SaveChangesAsync is called on the UnitOfWork.
        /// </remarks>
        Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes multiple entities.
        /// </summary>
        /// <param name="entities">The entities to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a queryable for advanced queries.
        /// </summary>
        /// <returns>An IQueryable for LINQ queries.</returns>
        /// <remarks>
        /// Use for complex queries with filtering, sorting, and projection.
        /// Changes to entities retrieved via Query() must be tracked separately.
        /// </remarks>
        IQueryable<TEntity> Query();

        /// <summary>
        /// Counts entities matching a predicate.
        /// </summary>
        /// <param name="predicate">Filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of matching entities.</returns>
        Task<int> CountAsync(
            Expression<Func<TEntity, bool>>? predicate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if any entity matches a predicate.
        /// </summary>
        /// <param name="predicate">Filter expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if any entity matches; otherwise, false.</returns>
        Task<bool> AnyAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paged subset of entities, optionally filtered and ordered.
        /// </summary>
        /// <param name="pagination">Page number and page size.</param>
        /// <param name="predicate">Optional filter expression.</param>
        /// <param name="orderBy">
        /// Optional ordering function applied to the queryable before paging.
        /// If null, no explicit ordering is applied (database-default order).
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="PagedResult{T}"/> containing the page of items and total count.</returns>
        /// <remarks>
        /// <para>
        /// Always prefer this over <see cref="GetAllAsync"/> for tables that may grow large
        /// (studies, instances, audit logs, queue items).
        /// </para>
        /// <para>
        /// <strong>Usage Example:</strong>
        /// <code>
        /// var page = await repo.GetPagedAsync(
        ///     new PaginationRequest { Page = 1, PageSize = 50 },
        ///     predicate: s => s.Status == StudyStatus.Completed,
        ///     orderBy: q => q.OrderByDescending(s => s.ReceivedAt));
        /// </code>
        /// </para>
        /// </remarks>
        Task<PagedResult<TEntity>> GetPagedAsync(
            PaginationRequest pagination,
            Expression<Func<TEntity, bool>>? predicate = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default);
    }
}
