// Layer: Infrastructure
// Responsibility: Calls OpenAI vision APIs to extract schedule data from images.
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlab.Application.Common.Interfaces;
using Greenlab.Application.DTOs;
using Greenlab.Infrastructure.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Greenlab.Infrastructure.Services;

public class OpenAiScheduleImageAnalysisService : IScheduleImageAnalysisService
{
    private readonly HttpClient _http;
    private readonly ScheduleVisionOptions _options;
    private readonly ILogger<OpenAiScheduleImageAnalysisService> _logger;

    public OpenAiScheduleImageAnalysisService(HttpClient http, IOptions<ScheduleVisionOptions> options, ILogger<OpenAiScheduleImageAnalysisService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ScheduleAnalysisResultDto> AnalyzeImageAsync(byte[] imageBytes, string imageMimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("ScheduleVision:ApiKey is not configured. Add your OpenAI API key to appsettings.");

        var base64 = Convert.ToBase64String(imageBytes);
        var mime = string.IsNullOrWhiteSpace(imageMimeType) ? "image/jpeg" : imageMimeType;
        var dataUrl = $"data:{mime};base64,{base64}";

        var prompt = """
            You extract data from an academic timetable / lab schedule image (Arabic or English).
            Return ONLY a single JSON object (no markdown fences) with this exact shape:
            {
              "labs": [ { "name": "string" } ],
              "sections": [ { "labName": "string", "dayOfWeek": "Monday", "startTime": "09:00", "endTime": "10:30", "courseName": "string or null" } ],
              "staffAssignments": [ { "personName": "string", "role": "Teacher" or "TeacherAssistant", "labName": "string", "dayOfWeek": "Monday", "startTime": "09:00", "endTime": "10:30" } ]
            }
            Rules:
            - dayOfWeek must be English: Monday,Tuesday,Wednesday,Thursday,Friday,Saturday,Sunday.
            - startTime/endTime 24h HH:mm or HH:mm:ss.
            - labName in sections and staffAssignments must match a lab "name" you list.
            - If text is unreadable, return empty arrays.
            - Optional key "notes": short string if you need to explain uncertainty.
            """;

        var body = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = dataUrl } }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI error {Status}: {Body}", response.StatusCode, responseText);
            throw new InvalidOperationException($"Vision API failed: {(int)response.StatusCode}");
        }

        using var document = JsonDocument.Parse(responseText);
        var message = document.RootElement.GetProperty("choices")[0].GetProperty("message");
        var content = ReadMessageContent(message);
        var json = ExtractJsonObject(content);
        if (string.IsNullOrWhiteSpace(json))
            return new ScheduleAnalysisResultDto { Notes = "Empty model response." };

        return ParseLoose(json);
    }

    private static string? ReadMessageContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
            return null;
        if (content.ValueKind == JsonValueKind.String)
            return content.GetString();
        if (content.ValueKind == JsonValueKind.Array)
        {
            var builder = new StringBuilder();
            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    builder.Append(text.GetString());
            }
            return builder.Length == 0 ? null : builder.ToString();
        }

        return null;
    }

    private static string? ExtractJsonObject(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var text = content.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = text.IndexOf('\n');
            if (firstNewLine >= 0) text = text[(firstNewLine + 1)..].Trim();
            var fence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (fence > 0) text = text[..fence].Trim();
        }

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start < 0 || end <= start ? text : text[start..(end + 1)];
    }

    private static ScheduleAnalysisResultDto ParseLoose(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loose = JsonSerializer.Deserialize<LooseRoot>(json, options);
        if (loose == null)
            return new ScheduleAnalysisResultDto();

        var result = new ScheduleAnalysisResultDto { Notes = loose.Notes };
        foreach (var lab in loose.Labs ?? new List<LooseLab>())
        {
            if (!string.IsNullOrWhiteSpace(lab.Name))
                result.Labs.Add(new ProposedLabDto { Name = lab.Name.Trim(), CvLabCode = lab.CvLabCode, DeviceKey = lab.DeviceKey });
        }

        foreach (var section in loose.Sections ?? new List<LooseSection>())
        {
            if (!TryDay(section.DayOfWeek, out var day) || !TryTime(section.StartTime, out var start) || !TryTime(section.EndTime, out var end) || string.IsNullOrWhiteSpace(section.LabName))
                continue;
            result.Sections.Add(new ProposedSectionDto { LabName = section.LabName.Trim(), DayOfWeek = day, StartTime = start, EndTime = end, CourseName = section.CourseName });
        }

        foreach (var staff in loose.StaffAssignments ?? new List<LooseStaff>())
        {
            if (!TryDay(staff.DayOfWeek, out var day) || !TryTime(staff.StartTime, out var start) || !TryTime(staff.EndTime, out var end) || string.IsNullOrWhiteSpace(staff.PersonName) || string.IsNullOrWhiteSpace(staff.LabName))
                continue;
            result.StaffAssignments.Add(new ProposedStaffFromImageDto { PersonName = staff.PersonName.Trim(), Role = string.IsNullOrWhiteSpace(staff.Role) ? "TeacherAssistant" : staff.Role.Trim(), LabName = staff.LabName.Trim(), DayOfWeek = day, StartTime = start, EndTime = end });
        }

        return result;
    }

    private static bool TryDay(string? value, out DayOfWeek day)
    {
        day = default;
        return !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value.Trim(), true, out day);
    }

    private static bool TryTime(string? value, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value.Trim();
        if (TimeSpan.TryParse(trimmed, out time)) return true;
        if (!DateTime.TryParse(trimmed, out var dateTime)) return false;
        time = dateTime.TimeOfDay;
        return true;
    }

    private sealed class LooseRoot
    {
        public List<LooseLab>? Labs { get; set; }
        public List<LooseSection>? Sections { get; set; }
        [JsonPropertyName("staffAssignments")]
        public List<LooseStaff>? StaffAssignments { get; set; }
        public string? Notes { get; set; }
    }

    private sealed class LooseLab
    {
        public string? Name { get; set; }
        public string? CvLabCode { get; set; }
        public string? DeviceKey { get; set; }
    }

    private sealed class LooseSection
    {
        public string? LabName { get; set; }
        public string? DayOfWeek { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? CourseName { get; set; }
    }

    private sealed class LooseStaff
    {
        public string? PersonName { get; set; }
        public string? Role { get; set; }
        public string? LabName { get; set; }
        public string? DayOfWeek { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
    }
}
