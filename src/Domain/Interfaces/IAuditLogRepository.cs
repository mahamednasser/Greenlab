// Layer: Domain
// Responsibility: Defines persistence operations for audit logs.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for audit log entries.
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
}
