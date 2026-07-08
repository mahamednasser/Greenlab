// Layer: Domain
// Responsibility: Defines the generic persistence contract for aggregate repositories.
namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides generic persistence operations for domain entities.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by the repository.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the current unit of work.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the current unit of work.
    /// </summary>
    void Remove(TEntity entity);
}
