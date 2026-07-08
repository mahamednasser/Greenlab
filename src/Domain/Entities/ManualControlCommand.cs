// Layer: Domain
// Responsibility: Defines the ManualControlCommand domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

public class ManualControlCommand
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;
    public int RequestedByUserId { get; set; }
    public User RequestedBy { get; set; } = null!;

    public ManualControlTarget Target { get; set; }
    public string RequestedValue { get; set; } = string.Empty;
    public ManualControlCommandStatus CommandStatus { get; set; } = ManualControlCommandStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public string? FailureReason { get; set; }
}
