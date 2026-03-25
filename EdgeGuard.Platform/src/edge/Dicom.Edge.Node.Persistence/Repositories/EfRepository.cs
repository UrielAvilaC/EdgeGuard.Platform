namespace Dicom.Edge.Node.Persistence.Repositories;

using Dicom.Edge.Common.Pagination;

/// <summary>
/// Generic EF Core implementation of <see cref="IRepository{TEntity}"/>.
/// Scoped to match the DbContext lifetime. Changes are tracked but not persisted
/// until <see cref="IUnitOfWork.SaveChangesAsync"/> is called.
/// </summary>
public sealed class EfRepository<TEntity>(
    EdgeNodeDbContext context,
    ILogger<EfRepository<TEntity>> logger)
    : IRepository<TEntity> where TEntity : class
{
    private static readonly string EntityName = typeof(TEntity).Name;
    private readonly DbSet<TEntity> _set = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var entity = await _set.FindAsync([id], ct);
        logger.LogDebug("Repository<{Entity}>.GetById({Id}): {Result}",
            EntityName, id, entity is not null ? "found" : "not found");
        return entity;
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        var result = await _set.AsNoTracking().ToListAsync(ct);
        logger.LogDebug("Repository<{Entity}>.GetAll: {Count} rows", EntityName, result.Count);
        return result;
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var result = await _set.AsNoTracking().Where(predicate).ToListAsync(ct);
        logger.LogDebug("Repository<{Entity}>.Find: {Count} rows matched", EntityName, result.Count);
        return result;
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var entity = await _set.AsNoTracking().FirstOrDefaultAsync(predicate, ct);
        logger.LogDebug("Repository<{Entity}>.FirstOrDefault: {Result}",
            EntityName, entity is not null ? "found" : "not found");
        return entity;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        var entry = await _set.AddAsync(entity, ct);
        logger.LogDebug("Repository<{Entity}>.Add: entity staged for insert", EntityName);
        return entry.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        var list = entities as ICollection<TEntity> ?? entities.ToList();
        await _set.AddRangeAsync(list, ct);
        logger.LogDebug("Repository<{Entity}>.AddRange: {Count} entities staged", EntityName, list.Count);
    }

    public Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        logger.LogDebug("Repository<{Entity}>.Update: entity staged for update", EntityName);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is not null)
        {
            _set.Remove(entity);
            logger.LogDebug("Repository<{Entity}>.Delete({Id}): entity staged for removal", EntityName, id);
        }
        else
        {
            logger.LogDebug("Repository<{Entity}>.Delete({Id}): entity not found — no-op", EntityName, id);
        }
    }

    public Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        _set.Remove(entity);
        logger.LogDebug("Repository<{Entity}>.Delete: entity staged for removal", EntityName);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        var list = entities as ICollection<TEntity> ?? entities.ToList();
        _set.RemoveRange(list);
        logger.LogDebug("Repository<{Entity}>.DeleteRange: {Count} entities staged", EntityName, list.Count);
        return Task.CompletedTask;
    }

    public IQueryable<TEntity> Query() => _set.AsQueryable();

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var count = predicate is null
            ? await _set.CountAsync(ct)
            : await _set.CountAsync(predicate, ct);
        logger.LogDebug("Repository<{Entity}>.Count: {Count}", EntityName, count);
        return count;
    }

    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var exists = await _set.AnyAsync(predicate, ct);
        logger.LogDebug("Repository<{Entity}>.Any: {Result}", EntityName, exists);
        return exists;
    }

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

        logger.LogDebug(
            "Repository<{Entity}>.GetPaged: page={Page}, pageSize={PageSize}, returned={Count}, total={Total}",
            EntityName, pagination.Page, pagination.PageSize, items.Count, totalCount);

        return new PagedResult<TEntity>
        {
            Items      = items,
            Page       = pagination.Page,
            PageSize   = pagination.PageSize,
            TotalCount = totalCount,
        };
    }
}
