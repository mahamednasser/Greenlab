// Layer: Domain
// Responsibility: Defines persistence operations for smart ACs.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

public interface ISmartAcRepository : IRepository<SmartAc>
{
    Task<IReadOnlyList<SmartAc>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SmartAc>> ListByLabIdAsync(int labId, CancellationToken cancellationToken = default);
    Task<SmartAc?> GetByLabAndIdAsync(int labId, int acId, CancellationToken cancellationToken = default);
    Task<SmartAc?> GetByIdWithLabAsync(int acId, CancellationToken cancellationToken = default);
}
