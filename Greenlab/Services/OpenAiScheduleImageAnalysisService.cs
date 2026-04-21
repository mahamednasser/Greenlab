using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenlab.DTOs;
using Greenlab.Options;
using Microsoft.Extensions.Options;

namespace Greenlab.Services;

public class OpenAiScheduleImageAnalysisService : IScheduleImageAnalysisService
{
    private readonly HttpClient _http;
    private readonly ScheduleVisionOptions _opt;
    private readonly ILogger<OpenAiScheduleImageAnalysisService> _logger;

    public OpenAiScheduleImageAnalysisService(
        HttpClient http,
        IOptions<ScheduleVisionOptions> opt,
        ILogger<OpenAiScheduleImageAnalysisService> logger)
    {
        _http = http;
        _opt = opt.Value;
        _logger = logger;
    }

    public async Task<ScheduleAnalysisResultDto> AnalyzeImageAsync(byte[] imageBytes, string imageMimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.ApiKey))
            throw new InvalidOperationException("ScheduleVision:ApiKey is not configured. Add your OpenAI API key to appsettings.");

        var b64 = Convert.ToBase64String(imageBytes);
        var mime = string.IsNullOrWhiteSpace(imageMimeType) ? "image/jpeg" : imageMimeType;
        var dataUrl = $"data:{mime};base64,{b64}";

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
            - labName in sections and staffAssignments must match a lab "name" you list (same spelling as on the image when possible).
            - If text is unreadable, return empty arrays.
            - Optional key "notes": short string if you need to explain uncertainty.
            """;

        var body = new
        {
            model = _opt.Model,
            max_tokens = _opt.MaxTokens,
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

        var url = $"{_opt.BaseUrl.TrimEnd('/')}/chat/completions";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        var respText = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI error {Status}: {Body}", resp.StatusCode, respText);
            throw new InvalidOperationException($"Vision API failed: {(int)resp.StatusCode}");
        }

        using var doc = JsonDocument.Parse(respText);
        var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
        var content = ReadMessageContent(message);
        var json = ExtractJsonObject(content);
        if (string.IsNullOrWhiteSpace(json))
            return new ScheduleAnalysisResultDto { Notes = "Empty model response." };

        return ParseLoose(json);
    }

    private static string? ReadMessageContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var c))
            return null;
        if (c.ValueKind == JsonValueKind.String)
            return c.GetString();
        if (c.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var part in c.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                    sb.Append(t.GetString());
            }
            return sb.Length == 0 ? null : sb.ToString();
        }

        return null;
    }

    private static string? ExtractJsonObject(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var t = content.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = t.IndexOf('\n');
            if (firstNl >= 0) t = t[(firstNl + 1)..].Trim();
            var fence = t.LastIndexOf("```", StringComparison.Ordinal);
            if (fence > 0) t = t[..fence].Trim();
        }

        var start = t.IndexOf('{');
        var end = t.LastIndexOf('}');
        if (start < 0 || end <= start) return t;
        return t[start..(end + 1)];
    }

    private static ScheduleAnalysisResultDto ParseLoose(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var loose = JsonSerializer.Deserialize<LooseRoot>(json, options);
        if (loose == null)
            return new ScheduleAnalysisResultDto();

        var result = new ScheduleAnalysisResultDto { Notes = loose.Notes };

        foreach (var l in loose.Labs ?? new List<LooseLab>())
        {
            if (string.IsNullOrWhiteSpace(l.Name)) continue;
            result.Labs.Add(new ProposedLabDto { Name = l.Name.Trim(), CvLabCode = l.CvLabCode, DeviceKey = l.DeviceKey });
        }

        foreach (var s in loose.Sections ?? new List<LooseSection>())
        {
            if (!TryDay(s.DayOfWeek, out var dow)) continue;
            if (!TryTime(s.StartTime, out var st) || !TryTime(s.EndTime, out var en)) continue;
            if (string.IsNullOrWhiteSpace(s.LabName)) continue;
            result.Sections.Add(new ProposedSectionDto
            {
                LabName = s.LabName.Trim(),
                DayOfWeek = dow,
                StartTime = st,
                EndTime = en,
                CourseName = s.CourseName
            });
        }

        foreach (var a in loose.StaffAssignments ?? new List<LooseStaff>())
        {
            if (!TryDay(a.DayOfWeek, out var dow)) continue;
            if (!TryTime(a.StartTime, out var st) || !TryTime(a.EndTime, out var en)) continue;
            if (string.IsNullOrWhiteSpace(a.PersonName) || string.IsNullOrWhiteSpace(a.LabName)) continue;
            result.StaffAssignments.Add(new ProposedStaffFromImageDto
            {
                PersonName = a.PersonName.Trim(),
                Role = string.IsNullOrWhiteSpace(a.Role) ? "TeacherAssistant" : a.Role.Trim(),
                LabName = a.LabName.Trim(),
                DayOfWeek = dow,
                StartTime = st,
                EndTime = en
            });
        }

        return result;
    }

    private static bool TryDay(string? s, out DayOfWeek dow)
    {
        dow = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        return Enum.TryParse(s.Trim(), true, out dow);
    }

    private static bool TryTime(string? s, out TimeSpan t)
    {
        t = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        var x = s.Trim();
        if (TimeSpan.TryParse(x, out t)) return true;
        if (DateTime.TryParse(x, out var dt))
        {
            t = dt.TimeOfDay;
            return true;
        }

        return false;
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
