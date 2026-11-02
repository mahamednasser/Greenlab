// Layer: Infrastructure
// Responsibility: Provides the EF Core database context for Greenlab persistence.
using Greenlab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<LabDevice> LabDevices => Set<LabDevice>();
    public DbSet<SmartAc> SmartAcs => Set<SmartAc>();
    public DbSet<LabSensor> LabSensors => Set<LabSensor>();
    public DbSet<LabControlOverride> LabControlOverrides => Set<LabControlOverride>();
    public DbSet<ManualControlCommand> ManualControlCommands => Set<ManualControlCommand>();
    public DbSet<LabSectionSchedule> LabSectionSchedules => Set<LabSectionSchedule>();
    public DbSet<StaffLabAssignment> StaffLabAssignments => Set<StaffLabAssignment>();
    public DbSet<LabChangeRequest> LabChangeRequests => Set<LabChangeRequest>();
    public DbSet<ScheduleImageImport> ScheduleImageImports => Set<ScheduleImageImport>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
