using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Services;

/// <summary>
/// Resolves hop IPs to geo coordinates via ip-api.com's free batch endpoint, caching every
/// result forever (a public IP's rough geolocation practically never changes) so the upstream
/// API is only hit for IPs never seen before.
/// </summary>
public class GeoIpService(TraceMonitorDbContext db, IHttpClientFactory httpClientFactory, ILogger<GeoIpService> logger) : IGeoIpService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyDictionary<string, IpGeoCache>> ResolveAsync(IReadOnlyCollection<string> ips, CancellationToken ct)
    {
        var distinct = ips.Distinct().ToList();
        var result = new Dictionary<string, IpGeoCache>();
        if (distinct.Count == 0)
            return result;

        var cached = await db.IpGeoCache.Where(g => distinct.Contains(g.Ip)).ToListAsync(ct);
        foreach (var c in cached)
            result[c.Ip] = c;

        var missing = distinct.Where(ip => !result.ContainsKey(ip)).ToList();
        if (missing.Count == 0)
            return result;

        var toQuery = new List<string>();
        foreach (var ip in missing)
        {
            if (IsPrivateOrReserved(ip))
            {
                var priv = new IpGeoCache { Ip = ip, IsPrivate = true };
                db.IpGeoCache.Add(priv);
                result[ip] = priv;
            }
            else
            {
                toQuery.Add(ip);
            }
        }

        if (toQuery.Count > 0)
            await QueryAndCacheAsync(toQuery, result, ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two concurrent resolutions raced to insert the same IP (e.g. a hop shared by
            // several targets in the same cycle) — whoever lost just re-reads what's there now.
            db.ChangeTracker.Clear();
            var nowCached = await db.IpGeoCache.Where(g => missing.Contains(g.Ip)).ToListAsync(ct);
            foreach (var c in nowCached)
                result[c.Ip] = c;
        }

        return result;
    }

    private async Task QueryAndCacheAsync(List<string> toQuery, Dictionary<string, IpGeoCache> result, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("ip-api");

        foreach (var chunk in toQuery.Chunk(100))
        {
            try
            {
                var payload = JsonSerializer.Serialize(
                    chunk.Select(ip => new { query = ip, fields = "status,country,city,lat,lon,isp,org,as,query" }));

                using var response = await client.PostAsync(
                    "batch", new StringContent(payload, Encoding.UTF8, "application/json"), ct);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync(ct);
                var items = JsonSerializer.Deserialize<List<IpApiResult>>(body, JsonOptions) ?? [];

                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item.Query))
                        continue;

                    var geo = new IpGeoCache
                    {
                        Ip = item.Query,
                        IsPrivate = false,
                        City = item.City,
                        Country = item.Country,
                        Isp = item.Isp,
                        Org = item.Org,
                        Asn = string.IsNullOrWhiteSpace(item.As) ? null : item.As,
                        Lat = item.Status == "success" ? item.Lat : null,
                        Lon = item.Status == "success" ? item.Lon : null,
                    };
                    db.IpGeoCache.Add(geo);
                    result[geo.Ip] = geo;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Fallo la resolucion geoip para {Count} ips", chunk.Length);
            }
        }
    }

    private static bool IsPrivateOrReserved(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr))
            return true;

        var b = addr.GetAddressBytes();
        if (b.Length != 4)
            return false; // only IPv4 hops are expected; treat anything else as public rather than guess

        return b[0] switch
        {
            10 => true,
            127 => true,
            169 when b[1] == 254 => true,
            172 when b[1] is >= 16 and <= 31 => true,
            192 when b[1] == 168 => true,
            100 when b[1] is >= 64 and <= 127 => true, // CGNAT
            _ => false,
        };
    }

    private class IpApiResult
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("query")]
        public string? Query { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("isp")]
        public string? Isp { get; set; }

        [JsonPropertyName("org")]
        public string? Org { get; set; }

        [JsonPropertyName("as")]
        public string? As { get; set; }

        [JsonPropertyName("lat")]
        public double? Lat { get; set; }

        [JsonPropertyName("lon")]
        public double? Lon { get; set; }
    }
}
