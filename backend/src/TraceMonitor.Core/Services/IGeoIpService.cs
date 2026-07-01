using TraceMonitor.Core.Models;

namespace TraceMonitor.Core.Services;

public interface IGeoIpService
{
    /// <summary>Resolves geo info for the given IPs, using the cache and only querying the
    /// upstream provider for IPs not seen before. Private/reserved IPs are never queried.</summary>
    Task<IReadOnlyDictionary<string, IpGeoCache>> ResolveAsync(IReadOnlyCollection<string> ips, CancellationToken ct);
}
