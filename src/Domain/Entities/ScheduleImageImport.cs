// Layer: Domain
// Responsibility: Defines the ScheduleImageImport domain entity.
using Greenlab.Domain.Enums;

namespace Greenlab.Domain.Entities;

/// <summary>Stores uploaded timetable images for admin; OCR can populate rows later.</summary>
public class ScheduleImageImport
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "image/jpeg";
    public long FileSizeBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public User UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
