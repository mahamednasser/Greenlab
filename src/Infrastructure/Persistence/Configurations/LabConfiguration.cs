// Layer: Infrastructure
// Responsibility: Configures EF Core mapping for lab-related entities.
using Greenlab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlab.Infrastructure.Persistence.Configurations;

public class LabConfiguration : IEntityTypeConfiguration<Lab>
{
    public void Configure(EntityTypeBuilder<Lab> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.DeviceKey).HasMaxLength(128);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.DeviceKey).IsUnique();
        builder.HasIndex(x => x.CvLabCode);
    }
}

public class LabDeviceConfiguration : IEntityTypeConfiguration<LabDevice>
{
    public void Configure(EntityTypeBuilder<LabDevice> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.DeviceKeyHash).HasMaxLength(128);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.DeviceType).HasConversion<int>();
        builder.HasOne(x => x.Lab).WithMany(x => x.Devices).HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.LabId);
        builder.HasIndex(x => x.DeviceKeyHash).IsUnique();
    }
}

public class SmartAcConfiguration : IEntityTypeConfiguration<SmartAc>
{
    public void Configure(EntityTypeBuilder<SmartAc> builder)
    {
        builder.ToTable("SmartAcs");
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.IpAddress).HasMaxLength(128);
        builder.Property(x => x.PathPrefix).HasMaxLength(100);
        builder.HasOne(x => x.Lab).WithMany(x => x.SmartAcs).HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.LabId);
        builder.HasIndex(x => new { x.LabId, x.IpAddress, x.Port });
    }
}

public class LabSensorConfiguration : IEntityTypeConfiguration<LabSensor>
{
    public void Configure(EntityTypeBuilder<LabSensor> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Unit).HasMaxLength(40);
        builder.Property(x => x.SensorType).HasConversion<int>();
        builder.HasOne(x => x.LabDevice).WithMany(x => x.Sensors).HasForeignKey(x => x.LabDeviceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Lab).WithMany(x => x.Sensors).HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LabId, x.SensorType });
    }
}

public class LabControlOverrideConfiguration : IEntityTypeConfiguration<LabControlOverride>
{
    public void Configure(EntityTypeBuilder<LabControlOverride> builder)
    {
        builder.HasKey(x => x.LabId);
        builder.Property(x => x.ControlMode).HasConversion<int>();
        builder.HasOne(x => x.Lab).WithOne(x => x.ControlOverride).HasForeignKey<LabControlOverride>(x => x.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.SetByUser).WithMany().HasForeignKey(x => x.SetByUserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.SetByUserId);
    }
}

public class ManualControlCommandConfiguration : IEntityTypeConfiguration<ManualControlCommand>
{
    public void Configure(EntityTypeBuilder<ManualControlCommand> builder)
    {
        builder.Property(x => x.Target).HasConversion<int>();
        builder.Property(x => x.CommandStatus).HasConversion<int>();
        builder.Property(x => x.RequestedValue).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.HasOne(x => x.Lab).WithMany(x => x.ManualControlCommands).HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RequestedBy).WithMany(x => x.ManualControlCommands).HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.LabId, x.RequestedAt });
        builder.HasIndex(x => x.RequestedByUserId);
    }
}

public class LabSectionScheduleConfiguration : IEntityTypeConfiguration<LabSectionSchedule>
{
    public void Configure(EntityTypeBuilder<LabSectionSchedule> builder)
    {
        builder.HasOne(x => x.Lab).WithMany(x => x.SectionSchedules).HasForeignKey(x => x.LabId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.CourseName).HasMaxLength(200);
        builder.Property(x => x.SectionName).HasMaxLength(100);
        builder.Property(x => x.AcademicTerm).HasMaxLength(100);
        builder.HasIndex(x => new { x.LabId, x.DayOfWeek, x.StartTime });
        builder.HasIndex(x => new { x.DayOfWeek, x.StartTime, x.EndTime });
    }
}
