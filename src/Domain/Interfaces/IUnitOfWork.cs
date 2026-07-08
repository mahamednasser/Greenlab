// Layer: Domain
// Responsibility: Defines the transaction boundary for persistence operations.
namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Coordinates saving pending repository changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a set of persistence operations inside a transaction.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
}
