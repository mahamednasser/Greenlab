// Layer: Domain
// Responsibility: Defines the AuditLog domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? LabId { get; set; }
    public Lab? Lab { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
