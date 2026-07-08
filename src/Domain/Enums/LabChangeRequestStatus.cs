// Layer: Domain
// Responsibility: Defines the LabChangeRequestStatus domain enumeration.
namespace Greenlab.Domain.Enums;

public enum LabChangeRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    AutoApproved = 3
}
