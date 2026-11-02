// Layer: Domain
// Responsibility: Defines the SmartAc domain entity (WiFi AC linked to a lab).
namespace Greenlab.Domain.Entities;

/// <summary>
/// Smart air conditioner with a WiFi module on the campus LAN, bound to one lab.
/// Backend proxies HTTP commands to <see cref="IpAddress"/> so the browser never talks to the LAN device directly.
/// </summary>
public class SmartAc
{
    public int Id { get; set; }
    public int LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    /// <summary>IPv4/hostname of the AC WiFi card (e.g. 192.168.1.55).</summary>
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    /// <summary>Optional path prefix, e.g. "/ac" → http://ip/ac/power/on</summary>
    public string? PathPrefix { get; set; }
    public bool IsActive { get; set; } = true;
    public int? LastTargetTempC { get; set; }
    public bool? LastPowerOn { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
