// Layer: Infrastructure
// Responsibility: Implements audit log persistence with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Interfaces;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
