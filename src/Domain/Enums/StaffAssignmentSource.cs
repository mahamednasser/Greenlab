// Layer: Domain
// Responsibility: Defines the StaffAssignmentSource domain enumeration.
namespace Greenlab.Domain.Enums;

public enum StaffAssignmentSource
{
    ScheduleImport = 0,
    ManualAdmin = 1,
    AutoApprovedTransfer = 2,
    AdminApprovedTransfer = 3
}
