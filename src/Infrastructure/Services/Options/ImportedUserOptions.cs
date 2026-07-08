// Layer: Infrastructure
// Responsibility: Defines configuration options for ImportedUserOptions infrastructure services.
namespace Greenlab.Infrastructure.Services.Options;

public class ImportedUserOptions
{
    public const string SectionName = "ImportedUsers";

    /// <summary>Initial password for users created from timetable image analysis (they should change it).</summary>
    public string DefaultPassword { get; set; } = "Imported123!";
}
