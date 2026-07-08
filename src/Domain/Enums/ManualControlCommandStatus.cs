// Layer: Domain
// Responsibility: Defines the ManualControlCommandStatus domain enumeration.
namespace Greenlab.Domain.Enums;

public enum ManualControlCommandStatus
{
    Pending = 0,
    Sent = 1,
    Acknowledged = 2,
    Failed = 3
}
