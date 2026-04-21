namespace Greenlab.Options;

public class ScheduleVisionOptions
{
    public const string SectionName = "ScheduleVision";

    /// <summary>OpenAI API key. If empty, image analysis endpoints return an error.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string Model { get; set; } = "gpt-4o-mini";

    public int MaxTokens { get; set; } = 4096;
}
