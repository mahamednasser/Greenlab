namespace Greenlab.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }

    public ICollection<StaffLabAssignment> StaffLabAssignments { get; set; } = new List<StaffLabAssignment>();
    public ICollection<LabChangeRequest> LabChangeRequestsRequested { get; set; } = new List<LabChangeRequest>();
    public ICollection<LabChangeRequest> LabChangeRequestsReviewed { get; set; } = new List<LabChangeRequest>();
    public ICollection<ScheduleImageImport> ScheduleImageImports { get; set; } = new List<ScheduleImageImport>();
}
