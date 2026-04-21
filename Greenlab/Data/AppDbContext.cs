using Greenlab.Models;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Lab> Labs => Set<Lab>();
    public DbSet<LabControlOverride> LabControlOverrides => Set<LabControlOverride>();
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
            e.HasIndex(x => x.DeviceKey);
        });

        modelBuilder.Entity<LabControlOverride>(e =>
        {
            e.HasKey(x => x.LabId);
            e.HasOne(x => x.Lab).WithOne(x => x.ControlOverride).HasForeignKey<LabControlOverride>(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LabSectionSchedule>(e =>
        {
            e.HasOne(x => x.Lab).WithMany(x => x.SectionSchedules).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.CourseName).HasMaxLength(200);
            e.HasIndex(x => new { x.LabId, x.DayOfWeek, x.StartTime });
        });

        modelBuilder.Entity<StaffLabAssignment>(e =>
        {
            e.HasOne(x => x.User).WithMany(x => x.StaffLabAssignments).HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Lab).WithMany(x => x.StaffAssignments).HasForeignKey(x => x.LabId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.DayOfWeek, x.StartTime });
            e.HasIndex(x => new { x.UserId, x.ValidFrom, x.ValidUntil });
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
        });
    }
}
