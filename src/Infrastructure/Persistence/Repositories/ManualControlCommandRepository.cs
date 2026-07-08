// Layer: Infrastructure
// Responsibility: Implements manual control command persistence with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class ManualControlCommandRepository : Repository<ManualControlCommand>, IManualControlCommandRepository
{
    public ManualControlCommandRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
