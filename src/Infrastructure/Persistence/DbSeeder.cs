// Layer: Infrastructure
// Responsibility: Seeds the database with initial users, labs, devices, and assignments.
using Greenlab.Application.Common;
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
            CvLabCode = "lab-a",
            LastOccupancyCount = 8,
            LastOccupancyAt = DateTime.UtcNow.AddMinutes(-4)
        };
        var lab2 = new Lab
        {
            Name = "Lab B",
            DeviceKey = "dev-key-lab-b-change-me",
            LightThreshold = 2000,
            TempFanOnThresholdCelsius = 28f,
            CvLabCode = "lab-b",
            LastOccupancyCount = 3,
            LastOccupancyAt = DateTime.UtcNow.AddMinutes(-12)
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
                LabId = lab1.Id,
                Name = "Lab A Motion",
                DeviceKeyHash = deviceKeyHasher.Hash(lab1.DeviceKey + "-motion"),
                DeviceType = LabDeviceType.MotionSensor
            },
            new LabDevice
            {
                LabId = lab2.Id,
                Name = "Lab B ESP32",
                DeviceKeyHash = deviceKeyHasher.Hash(lab2.DeviceKey),
                DeviceType = LabDeviceType.Esp32Controller
            },
            new LabDevice
            {
                LabId = lab2.Id,
                Name = "Lab B Motion",
                DeviceKeyHash = deviceKeyHasher.Hash(lab2.DeviceKey + "-motion"),
                DeviceType = LabDeviceType.MotionSensor
            });

        // Weekly sections so "labs in session" works any day.
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            db.LabSectionSchedules.Add(new LabSectionSchedule
            {
                LabId = lab1.Id,
                DayOfWeek = day,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(20),
                CourseName = day is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Open Lab" : "IoT 101"
            });
            db.LabSectionSchedules.Add(new LabSectionSchedule
            {
                LabId = lab2.Id,
                DayOfWeek = day,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17),
                CourseName = day is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Maker Space" : "Networks Lab"
            });
        }

        // Staff can see labs any day during demo hours.
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            db.StaffLabAssignments.Add(new StaffLabAssignment
            {
                UserId = teacher.Id,
                LabId = lab1.Id,
                DayOfWeek = day,
                StartTime = TimeSpan.FromHours(0),
                EndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)),
                AssignmentSource = StaffAssignmentSource.ScheduleImport
            });
            db.StaffLabAssignments.Add(new StaffLabAssignment
            {
                UserId = assistant.Id,
                LabId = lab2.Id,
                DayOfWeek = day,
                StartTime = TimeSpan.FromHours(0),
                EndTime = TimeSpan.FromHours(23).Add(TimeSpan.FromMinutes(59)),
                AssignmentSource = StaffAssignmentSource.ScheduleImport
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fills occupancy / schedules / staff access / in-memory energy so the dashboard looks populated.
    /// Safe to run on every startup; skips rows that already exist and does not overwrite live cache state.
    /// </summary>
    public static async Task EnsureDemoDashboardAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var labs = await db.Labs.AsTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        if (labs.Count == 0)
            return;

        var nowUtc = DateTime.UtcNow;
        var changed = false;

        // Demo occupancy readings for alerts / activity.
        foreach (var lab in labs)
        {
            if (lab.LastOccupancyAt != null)
                continue;

            lab.LastOccupancyCount = lab.Id % 2 == 0 ? 3 : 8;
            lab.LastOccupancyAt = nowUtc.AddMinutes(-(5 + lab.Id * 3));
            changed = true;
        }

        var existingSchedules = await db.LabSectionSchedules
            .Select(s => new { s.LabId, s.DayOfWeek })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var scheduleKeys = existingSchedules
            .Select(s => (s.LabId, s.DayOfWeek))
            .ToHashSet();

        foreach (var lab in labs)
        {
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                if (scheduleKeys.Contains((lab.Id, day)))
                    continue;

                db.LabSectionSchedules.Add(new LabSectionSchedule
                {
                    LabId = lab.Id,
                    DayOfWeek = day,
                    StartTime = TimeSpan.FromHours(8),
                    EndTime = TimeSpan.FromHours(20),
                    CourseName = "Demo Session"
                });
                changed = true;
            }
        }

        // Temporary window so staff dashboards work even outside weekly slots.
        var staffUsers = await db.Users
            .Where(u => u.Role == Role.Teacher || u.Role == Role.TeacherAssistant)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var demoFrom = nowUtc.AddDays(-1);
        var demoUntil = nowUtc.AddDays(30);
        var existingTemp = await db.StaffLabAssignments
            .Where(a => a.ValidFrom != null && a.ValidUntil != null && a.IsActive)
            .Select(a => new { a.UserId, a.LabId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var tempKeys = existingTemp.Select(a => (a.UserId, a.LabId)).ToHashSet();

        foreach (var user in staffUsers)
        {
            foreach (var lab in labs)
            {
                if (tempKeys.Contains((user.Id, lab.Id)))
                    continue;

                db.StaffLabAssignments.Add(new StaffLabAssignment
                {
                    UserId = user.Id,
                    LabId = lab.Id,
                    ValidFrom = demoFrom,
                    ValidUntil = demoUntil,
                    AssignmentSource = StaffAssignmentSource.ScheduleImport,
                    IsActive = true
                });
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // In-memory energy/runtime for dashboard charts only — keep light/fan OFF
        // so the labs page does not show devices as running when they are not.
        for (var i = 0; i < labs.Count; i++)
        {
            var lab = labs[i];
            if (LabDeviceStateCache.HasState(lab.Id))
                continue;

            var profile = i % 3;
            switch (profile)
            {
                case 0:
                    LabDeviceStateCache.TrySeedDemo(
                        lab.Id,
                        lightOn: false,
                        fanOn: false,
                        lightOnSeconds: 2.5 * 3600,
                        fanOnSeconds: 1.8 * 3600);
                    break;
                case 1:
                    LabDeviceStateCache.TrySeedDemo(
                        lab.Id,
                        lightOn: false,
                        fanOn: false,
                        lightOnSeconds: 4.0 * 3600,
                        fanOnSeconds: 0.75 * 3600);
                    break;
                default:
                    LabDeviceStateCache.TrySeedDemo(
                        lab.Id,
                        lightOn: false,
                        fanOn: false,
                        lightOnSeconds: 1.2 * 3600,
                        fanOnSeconds: 0.5 * 3600);
                    break;
            }
        }
    }
}
