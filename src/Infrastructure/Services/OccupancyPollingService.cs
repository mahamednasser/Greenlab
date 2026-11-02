using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Greenlab.Application.Common;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Domain.Interfaces;
using Greenlab.Domain.Entities;
using Greenlab.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace Greenlab.Infrastructure.Services;

public class OccupancyPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OccupancyPollingService> _logger;

    public OccupancyPollingService(IServiceProvider serviceProvider, ILogger<OccupancyPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Occupancy Polling Service has started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollActiveLabsOccupancyAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during occupancy polling.");
            }

            // Poll every 15 seconds while any lab has light/fan ON
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Occupancy Polling Service is stopping.");
    }

    private async Task PollActiveLabsOccupancyAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crowdClient = scope.ServiceProvider.GetRequiredService<ICrowdCountClient>();
        var auditLogs = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var labs = await db.Labs
            .Include(l => l.ControlOverride)
            .ToListAsync(stoppingToken).ConfigureAwait(false);
            
        var now = DateTime.UtcNow;

        foreach (var lab in labs)
        {
            var cachedState = LabDeviceStateCache.GetState(lab.Id);
            var lightOn = lab.ControlOverride?.ForceLightOn ?? cachedState.LightOn;
            var fanOn = lab.ControlOverride?.ForceFanOn ?? cachedState.FanOn;

            // Poll when light or fan is ON
            if (!lightOn && !fanOn)
                continue;

            _logger.LogInformation(
                "Polling occupancy for lab {LabName} (ID: {LabId}) because light={LightOn}, fan={FanOn}.",
                lab.Name, lab.Id, lightOn, fanOn);

            try
            {
                var count = await crowdClient.GetOccupancyCountAsync(lab, stoppingToken).ConfigureAwait(false);

                lab.LastOccupancyCount = count;
                lab.LastOccupancyAt = now;

                var isOverrideLight = lab.ControlOverride?.ForceLightOn;
                var isOverrideFan = lab.ControlOverride?.ForceFanOn;

                // Locked motion: keep last light/fan; unlocked: turn off when empty.
                bool finalLight;
                bool finalFan;
                if (lab.ControlOverride?.MotionSensorDisabled == true)
                {
                    finalLight = isOverrideLight ?? cachedState.LightOn;
                    finalFan = isOverrideFan ?? cachedState.FanOn;
                }
                else
                {
                    finalLight = isOverrideLight ?? (count > 0 && cachedState.LightOn);
                    finalFan = isOverrideFan ?? (count > 0 && cachedState.FanOn);
                }

                LabDeviceStateCache.SetState(lab.Id, finalLight, finalFan);

                var detailsObject = new
                {
                    الحدث = new
                    {
                        تنبيه_المراقبة = "طلب استعلام دوري تلقائي (كل 15 ثانية)",
                        اسم_المختبر = lab.Name,
                        حالة_الإضاءة = lightOn ? "مشتغلة" : "مغلقة",
                        حالة_المروحة = fanOn ? "مشتغلة" : "مغلقة"
                    },
                    النتيجة = new
                    {
                        العدد_الفعلي_للأشخاص = count,
                        حالة_الإضاءة_الجديدة = finalLight ? "تشغيل" : "إطفاء",
                        حالة_المروحة_الجديدة = finalFan ? "تشغيل" : "إطفاء"
                    }
                };

                var detailsJson = JsonSerializer.Serialize(detailsObject, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                await auditLogs.AddAsync(new AuditLog
                {
                    Action = "PeriodicOccupancyCheck",
                    LabId = lab.Id,
                    UserId = null,
                    Details = detailsJson,
                    CreatedAt = now
                }, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to poll occupancy for lab {LabName}.", lab.Name);
            }
        }

        await unitOfWork.SaveChangesAsync(stoppingToken).ConfigureAwait(false);
    }
}
