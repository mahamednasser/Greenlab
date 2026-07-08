// Layer: Infrastructure
// Responsibility: Seeds the database with initial users, labs, devices, and assignments.
using Greenlab.Application.Common.Interfaces;
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHashService passwordHashService, IDeviceKeyHasher deviceKeyHasher, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        if (await db.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
            return;

        var admin = new User
        {
            Email = "admin@greenlab.local",
            FullName = "Admin User",
            Role = Role.Admin
        };
        admin.PasswordHash = passwordHashService.HashPassword(admin, "Admin123!");

        var teacher = new User
        {
            Email = "teacher@greenlab.local",
            FullName = "Teacher User",
            Role = Role.Teacher
        };
        teacher.PasswordHash = passwordHashService.HashPassword(teacher, "Teacher123!");

        var assistant = new User
        {
            Email = "assistant@greenlab.local",
            FullName = "TA User",
            Role = Role.TeacherAssistant
        };
        assistant.PasswordHash = passwordHashService.HashPassword(assistant, "Assistant123!");

        db.Users.AddRange(admin, teacher, assistant);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var lab1 = new Lab
        {
            Name = "Lab A",
            DeviceKey = "dev-key-lab-a-change-me",
            LightThreshold = 2000,
            TempFanOnThresholdCelsius = 28f,
            CvLabCode = "lab-a"
        };
        var lab2 = new Lab
        {
            Name = "Lab B",
            DeviceKey = "dev-key-lab-b-change-me",
            LightThreshold = 2000,
            TempFanOnThresholdCelsius = 28f,
            CvLabCode = "lab-b"
        };
        db.Labs.AddRange(lab1, lab2);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        db.LabControlOverrides.AddRange(
            new LabControlOverride { LabId = lab1.Id, UpdatedAt = DateTime.UtcNow },
            new LabControlOverride { LabId = lab2.Id, UpdatedAt = DateTime.UtcNow });

        db.LabDevices.AddRange(
            new LabDevice
            {
                LabId = lab1.Id,
                Name = "Lab A ESP32",
                DeviceKeyHash = deviceKeyHasher.Hash(lab1.DeviceKey),
                DeviceType = LabDeviceType.Esp32Controller
            },
            new LabDevice
            {
                LabId = lab2.Id,
                Name = "Lab B ESP32",
                DeviceKeyHash = deviceKeyHasher.Hash(lab2.DeviceKey),
                DeviceType = LabDeviceType.Esp32Controller
            });

        db.LabSectionSchedules.Add(new LabSectionSchedule
        {
            LabId = lab1.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(12),
            CourseName = "IoT 101"
        });

        db.StaffLabAssignments.AddRange(
            new StaffLabAssignment
            {
                UserId = teacher.Id,
                LabId = lab1.Id,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17)
            },
            new StaffLabAssignment
            {
                UserId = assistant.Id,
                LabId = lab1.Id,
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(14)
            });

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
