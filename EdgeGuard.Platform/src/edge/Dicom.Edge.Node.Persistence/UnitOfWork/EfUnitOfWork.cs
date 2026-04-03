using Microsoft.EntityFrameworkCore.Storage;

namespace Dicom.Edge.Node.Persistence.UnitOfWork;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/>.
/// Scoped lifetime — shares the same <see cref="EdgeNodeDbContext"/> instance as
/// all <see cref="EfRepository{T}"/> instances registered in the same DI scope.
/// </summary>
public sealed class EfUnitOfWork(EdgeNodeDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public bool HasActiveTransaction => _transaction is not null;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => context.SaveChangesAsync(cancellationToken);

    public async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null) return false;
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return true;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await context.SaveChangesAsync(cancellationToken);
        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        // Do NOT dispose context — it is DI-managed (Scoped lifetime).
        // Disposing here would break other repositories sharing the same scope.
    }
}
