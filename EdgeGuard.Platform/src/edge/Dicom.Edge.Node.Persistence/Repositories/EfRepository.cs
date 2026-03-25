namespace Dicom.Edge.Node.Persistence.Repositories;

using Dicom.Edge.Common.Pagination;

/// <summary>
/// Generic EF Core implementation of <see cref="IRepository{TEntity}"/>.
/// Scoped to match the DbContext lifetime. Changes are tracked but not persisted
/// until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
/// </summary>
public sealed class EfRepository<TEntity>(EdgeNodeDbContext context)
    : IRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _set = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken ct = default)
        => await _set.FindAsync([id], ct);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await _set.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        var entry = await _set.AddAsync(entity, ct);
        return entry.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => await _set.AddRangeAsync(entities, ct);

    public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null) _set.Remove(entity);
    }

    public Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        _set.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        _set.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public IQueryable<TEntity> Query() => _set.AsQueryable();

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
        => predicate is null
            ? await _set.CountAsync(ct)
            : await _set.CountAsync(predicate, ct);

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _set.AnyAsync(predicate, ct);

    public async Task<PagedResult<TEntity>> GetPagedAsync(
        PaginationRequest pagination,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default)
    {
        IQueryable<TEntity> query = _set.AsNoTracking();

        if (predicate is not null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(ct);

        if (orderBy is not null)
            query = orderBy(query);

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TEntity>
        {
            Items      = items,
            Page       = pagination.Page,
            PageSize   = pagination.PageSize,
            TotalCount = totalCount,
        };
    }
}
