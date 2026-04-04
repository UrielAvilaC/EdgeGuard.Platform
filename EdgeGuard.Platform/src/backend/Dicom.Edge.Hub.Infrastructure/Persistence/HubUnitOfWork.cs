using Dicom.Edge.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Dicom.Edge.Hub.Infrastructure.Persistence;

/// <summary>
/// Unit of Work implementation for the Hub database.
/// </summary>
public class HubUnitOfWork : IUnitOfWork
{
    private readonly HubDbContext _context;
    private readonly ILogger<HubUnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;

    public HubUnitOfWork(HubDbContext context, ILogger<HubUnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public bool HasActiveTransaction => _currentTransaction is not null;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            _logger.LogWarning("Transaction already active");
            return false;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return true;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to rollback.");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
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
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}
