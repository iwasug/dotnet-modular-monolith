using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ModularMonolith.Infrastructure.Data;
using ModularMonolith.Infrastructure.Interfaces;

namespace ModularMonolith.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing database transactions with automatic commit/rollback
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
        {
            _logger.LogWarning("Transaction already exists. Returning existing transaction.");
            return _currentTransaction;
        }

        _logger.LogDebug("Beginning new database transaction");
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        return _currentTransaction;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving changes to database");
        
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            _logger.LogWarning("No active transaction to commit");
            return;
        }

        try
        {
            _logger.LogDebug("Committing database transaction");
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing transaction");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            _logger.LogWarning("No active transaction to rollback");
            return;
        }

        try
        {
            _logger.LogDebug("Rolling back database transaction");
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back transaction");
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        var transactionStarted = false;
        
        try
        {
            // Only start a new transaction if one doesn't already exist
            if (_currentTransaction is null)
            {
                await BeginTransactionAsync(cancellationToken);
                transactionStarted = true;
            }

            _logger.LogDebug("Executing operation within transaction");
            var result = await operation();
            
            if (transactionStarted)
            {
                await SaveChangesAsync(cancellationToken);
                await CommitTransactionAsync(cancellationToken);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing operation within transaction");
            
            if (transactionStarted)
            {
                await RollbackTransactionAsync(cancellationToken);
            }
            
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        await ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _currentTransaction?.Dispose();
            _disposed = true;
        }
    }
}