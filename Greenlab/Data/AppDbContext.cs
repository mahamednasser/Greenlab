using Greenlab.Models;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<LabDevice> LabDevices => Set<LabDevice>();
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
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.FullName).HasMaxLength(256);
            e.Property(x => x.Role).HasConversion<int>();
        });

        modelBuilder.Entity<Lab>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.DeviceKey).HasMaxLength(128);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.DeviceKey).IsUnique();
            e.HasIndex(x => x.CvLabCode);
        });

        modelBuilder.Entity<LabDevice>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.DeviceKeyHash).HasMaxLength(128);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.DeviceType).HasConversion<int>();
            e.HasOne(x => x.Lab).WithMany(x => x.Devices).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.LabId);
            e.HasIndex(x => x.DeviceKeyHash).IsUnique();
        });

        modelBuilder.Entity<LabSensor>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Unit).HasMaxLength(40);
            e.Property(x => x.SensorType).HasConversion<int>();
            e.HasOne(x => x.LabDevice).WithMany(x => x.Sensors).HasForeignKey(x => x.LabDeviceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lab).WithMany(x => x.Sensors).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.LabId, x.SensorType });
        });

        modelBuilder.Entity<LabControlOverride>(e =>
        {
            e.HasKey(x => x.LabId);
            e.Property(x => x.ControlMode).HasConversion<int>();
            e.HasOne(x => x.Lab).WithOne(x => x.ControlOverride).HasForeignKey<LabControlOverride>(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SetByUser).WithMany().HasForeignKey(x => x.SetByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.SetByUserId);
        });

        modelBuilder.Entity<ManualControlCommand>(e =>
        {
            e.Property(x => x.Target).HasConversion<int>();
            e.Property(x => x.CommandStatus).HasConversion<int>();
            e.Property(x => x.RequestedValue).HasMaxLength(100);
            e.Property(x => x.FailureReason).HasMaxLength(1000);
            e.HasOne(x => x.Lab).WithMany(x => x.ManualControlCommands).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RequestedBy).WithMany(x => x.ManualControlCommands).HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.LabId, x.RequestedAt });
            e.HasIndex(x => x.RequestedByUserId);
        });

        modelBuilder.Entity<LabSectionSchedule>(e =>
        {
            e.HasOne(x => x.Lab).WithMany(x => x.SectionSchedules).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.CourseName).HasMaxLength(200);
            e.Property(x => x.SectionName).HasMaxLength(100);
            e.Property(x => x.AcademicTerm).HasMaxLength(100);
            e.HasIndex(x => new { x.LabId, x.DayOfWeek, x.StartTime });
            e.HasIndex(x => new { x.DayOfWeek, x.StartTime, x.EndTime });
        });

        modelBuilder.Entity<StaffLabAssignment>(e =>
        {
            e.Property(x => x.AssignmentSource).HasConversion<int>();
            e.HasOne(x => x.User).WithMany(x => x.StaffLabAssignments).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lab).WithMany(x => x.StaffAssignments).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.DayOfWeek, x.StartTime });
            e.HasIndex(x => new { x.UserId, x.ValidFrom, x.ValidUntil });
            e.HasIndex(x => new { x.LabId, x.DayOfWeek, x.StartTime });
        });

        modelBuilder.Entity<LabChangeRequest>(e =>
        {
            e.Property(x => x.Status).HasConversion<int>();
            e.HasOne(x => x.RequestedBy).WithMany(x => x.LabChangeRequestsRequested)
                .HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReviewedBy).WithMany(x => x.LabChangeRequestsReviewed)
                .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.FromLab).WithMany().HasForeignKey(x => x.FromLabId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToLab).WithMany().HasForeignKey(x => x.ToLabId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ScheduleImageImport>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(512);
            e.Property(x => x.StoredPath).HasMaxLength(1024);
            e.HasOne(x => x.UploadedBy).WithMany(x => x.ScheduleImageImports).HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.Details).HasMaxLength(2000);
            e.HasOne(x => x.Lab).WithMany().HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasIndex(x => new { x.LabId, x.CreatedAt });
            e.HasIndex(x => x.Action);
        });
    }
}
