// Layer: Infrastructure
// Responsibility: Defines configuration options for StorageOptions infrastructure services.
namespace Greenlab.Infrastructure.Services.Options;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string ScheduleUploadsPath { get; set; } = "uploads/schedules";
}
