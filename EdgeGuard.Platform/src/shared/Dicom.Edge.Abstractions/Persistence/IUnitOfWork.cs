namespace Dicom.Edge.Abstractions.Persistence
{
    /// <summary>
    /// Defines a unit of work for managing database transactions and coordinating changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Unit of Work pattern maintains a list of objects affected by a business transaction
    /// and coordinates the writing out of changes and resolution of concurrency problems.
    /// </para>
    /// <para>
    /// <strong>Key Responsibilities:</strong>
    /// <list type="bullet">
    ///   <item><description>Track all changes made during a transaction</description></item>
    ///   <item><description>Commit or rollback all changes as a single atomic operation</description></item>
    ///   <item><description>Manage database transactions</description></item>
    ///   <item><description>Coordinate repositories</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// using var unitOfWork = serviceProvider.GetRequiredService&lt;IUnitOfWork&gt;();
    /// try
    /// {
    ///     await unitOfWork.BeginTransactionAsync();
    ///     
    ///     var study = await studyRepository.GetByIdAsync(studyUid);
    ///     study.Status = StudyStatus.Completed;
    ///     await studyRepository.UpdateAsync(study);
    ///     
    ///     var metrics = new StudyMetrics { StudyInstanceUid = studyUid };
    ///     await metricsRepository.AddAsync(metrics);
    ///     
    ///     await unitOfWork.SaveChangesAsync();
    ///     await unitOfWork.CommitTransactionAsync();
    /// }
    /// catch
    /// {
    ///     await unitOfWork.RollbackTransactionAsync();
    ///     throw;
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        /// <remarks>
        /// <para>
        /// This method writes all tracked changes to the database.
        /// If a transaction is active, changes are not committed until CommitTransactionAsync is called.
        /// </para>
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new database transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if transaction was started; false if already in a transaction.</returns>
        /// <remarks>
        /// <para>
        /// All database operations performed after this call will be part of the transaction
        /// until CommitTransactionAsync or RollbackTransactionAsync is called.
        /// </para>
        /// <para>
        /// Transactions provide ACID guarantees:
        /// <list type="bullet">
        ///   <item><description><strong>Atomicity:</strong> All operations succeed or all fail</description></item>
        ///   <item><description><strong>Consistency:</strong> Database remains in valid state</description></item>
        ///   <item><description><strong>Isolation:</strong> Concurrent transactions don't interfere</description></item>
        ///   <item><description><strong>Durability:</strong> Committed changes are permanent</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        Task<bool> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction, persisting all changes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// <para>
        /// All changes saved via SaveChangesAsync within this transaction become permanent.
        /// If commit fails, an exception is thrown and changes may be rolled back.
        /// </para>
        /// </remarks>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction, discarding all changes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// <para>
        /// All changes made within this transaction are discarded.
        /// The database is returned to the state before BeginTransactionAsync was called.
        /// </para>
        /// <para>
        /// Call this in exception handlers to undo partial changes.
        /// </para>
        /// </remarks>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets whether a transaction is currently active.
        /// </summary>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Executes a database operation with automatic transaction management.
        /// </summary>
        /// <typeparam name="TResult">The return type of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        /// <remarks>
        /// <para>
        /// Automatically begins a transaction, executes the operation, and commits on success.
        /// Rolls back on exception.
        /// </para>
        /// <para>
        /// <strong>Example:</strong>
        /// <code>
        /// var result = await unitOfWork.ExecuteInTransactionAsync(async () =>
        /// {
        ///     await repo1.AddAsync(entity1);
        ///     await repo2.UpdateAsync(entity2);
        ///     await unitOfWork.SaveChangesAsync();
        ///     return entity1.Id;
        /// });
        /// </code>
        /// </para>
        /// </remarks>
        Task<TResult> ExecuteInTransactionAsync<TResult>(
            Func<Task<TResult>> operation,
            CancellationToken cancellationToken = default);
    }
}
