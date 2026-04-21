namespace Greenlab.Models;

public class LabChangeRequest
{
    public int Id { get; set; }
    public int RequestedByUserId { get; set; }
    public User RequestedBy { get; set; } = null!;
    public int FromLabId { get; set; }
    public Lab FromLab { get; set; } = null!;
    public int ToLabId { get; set; }
    public Lab ToLab { get; set; } = null!;

    public DateTime SectionStart { get; set; }
    public DateTime SectionEnd { get; set; }

    public LabChangeRequestStatus Status { get; set; } = LabChangeRequestStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
