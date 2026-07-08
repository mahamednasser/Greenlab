// Layer: Infrastructure
// Responsibility: Configures EF Core mappings for staff assignments, schedule imports, and audit logs.
using Greenlab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlab.Infrastructure.Persistence.Configurations;

public class StaffLabAssignmentConfiguration : IEntityTypeConfiguration<StaffLabAssignment>
{
    public void Configure(EntityTypeBuilder<StaffLabAssignment> builder)
    {
        builder.Property(x => x.AssignmentSource).HasConversion<int>();
        builder.HasOne(x => x.User).WithMany(x => x.StaffLabAssignments).HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Lab).WithMany(x => x.StaffAssignments).HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.UserId, x.DayOfWeek, x.StartTime });
        builder.HasIndex(x => new { x.UserId, x.ValidFrom, x.ValidUntil });
        builder.HasIndex(x => new { x.LabId, x.DayOfWeek, x.StartTime });
    }
}

public class LabChangeRequestConfiguration : IEntityTypeConfiguration<LabChangeRequest>
{
    public void Configure(EntityTypeBuilder<LabChangeRequest> builder)
    {
        builder.Property(x => x.Status).HasConversion<int>();
        builder.HasOne(x => x.RequestedBy).WithMany(x => x.LabChangeRequestsRequested)
            .HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedBy).WithMany(x => x.LabChangeRequestsReviewed)
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.FromLab).WithMany().HasForeignKey(x => x.FromLabId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ToLab).WithMany().HasForeignKey(x => x.ToLabId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScheduleImageImportConfiguration : IEntityTypeConfiguration<ScheduleImageImport>
{
    public void Configure(EntityTypeBuilder<ScheduleImageImport> builder)
    {
        builder.Property(x => x.FileName).HasMaxLength(512);
        builder.Property(x => x.StoredPath).HasMaxLength(1024);
        builder.HasOne(x => x.UploadedBy).WithMany(x => x.ScheduleImageImports).HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(x => x.Action).HasMaxLength(100);
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.HasOne(x => x.Lab).WithMany().HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.LabId, x.CreatedAt });
        builder.HasIndex(x => x.Action);
    }
}
