namespace Greenlab.Options;

public class ImportedUserOptions
{
    public const string SectionName = "ImportedUsers";

    /// <summary>Initial password for users created from timetable image analysis (they should change it).</summary>
    public string DefaultPassword { get; set; } = "Imported123!";
}
