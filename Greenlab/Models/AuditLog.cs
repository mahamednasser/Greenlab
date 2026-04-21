namespace Greenlab.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? LabId { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
