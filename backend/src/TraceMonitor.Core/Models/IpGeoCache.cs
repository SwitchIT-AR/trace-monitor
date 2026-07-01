namespace TraceMonitor.Core.Models;

public class IpGeoCache
{
    public required string Ip { get; set; }
    public bool IsPrivate { get; set; }

    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Isp { get; set; }
    public string? Org { get; set; }

    /// <summary>Raw "as" field from ip-api.com, e.g. "AS7303 Telecom Argentina S.A." — stored
    /// verbatim, never inferred, so it's null whenever the provider doesn't have it.</summary>
    public string? Asn { get; set; }

    public double? Lat { get; set; }
    public double? Lon { get; set; }

    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
}
