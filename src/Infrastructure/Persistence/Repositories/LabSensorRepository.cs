// Layer: Infrastructure
// Responsibility: Implements lab sensor persistence with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class LabSensorRepository : Repository<LabSensor>, ILabSensorRepository
{
    public LabSensorRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
