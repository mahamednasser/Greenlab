// Layer: Application
// Responsibility: Resolves the campus schedule clock (local wall time) for staff access checks.
using System.Collections.Concurrent;

namespace Greenlab.Application.Common;

/// <summary>
/// Weekly staff schedules are entered as local wall-clock times (e.g. 19:00–20:00 Egypt).
/// Temporary ValidFrom/ValidUntil windows are stored as UTC instants.
/// </summary>
public static class ScheduleClock
{
    private static readonly ConcurrentDictionary<string, TimeZoneInfo> Cache = new();
    private static string _configuredId = DefaultTimeZoneId;

    /// <summary>IANA id preferred; Windows id used as fallback.</summary>
    public const string DefaultTimeZoneId = "Africa/Cairo";
    public const string WindowsTimeZoneId = "Egypt Standard Time";

    public static void Configure(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
            _configuredId = timeZoneId.Trim();
    }

    public static TimeZoneInfo Resolve(string? configuredId = null)
    {
        var key = string.IsNullOrWhiteSpace(configuredId) ? _configuredId : configuredId.Trim();
        return Cache.GetOrAdd(key, ResolveUncached);
    }

    public static DateTime ToLocal(DateTime utcNow, string? configuredId = null)
    {
        var utc = utcNow.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)
            : utcNow.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Resolve(configuredId));
    }

    private static TimeZoneInfo ResolveUncached(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            // ignore and try fallbacks
        }
        catch (InvalidTimeZoneException)
        {
            // ignore and try fallbacks
        }

        foreach (var fallback in new[] { DefaultTimeZoneId, WindowsTimeZoneId })
        {
            if (string.Equals(fallback, id, StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(fallback);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
