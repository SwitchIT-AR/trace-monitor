using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Services;

/// <summary>
/// Shared by both the in-process office worker (<c>TraceSchedulerWorker</c>) and the agent
/// ingestion endpoint, so a route reported from the office and one reported by a remote agent
/// against the same target are persisted and compared identically.
/// </summary>
public class TraceIngestionService(
    TraceMonitorDbContext db,
    IPathChangeDetector detector,
    IGeoIpService geoIp,
    ILogger<TraceIngestionService> logger) : ITraceIngestionService
{
    // The frontend consumes PreviousHopsJson/NewHopsJson directly (they're opaque strings to the
    // API layer, so ASP.NET Core's own camelCase policy never touches them) — serialize with the
    // same casing so both sides agree on the hop snapshot shape.
    private static readonly JsonSerializerOptions HopSnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<string>> IngestAsync(int agentId, int targetId, int packetsSent, MtrReport report, CancellationToken ct)
    {
        var pathHash = detector.ComputePathHash(report.Hops);

        // Route changes are scoped per (target, agent): two agents in different locations
        // legitimately see different, stable paths to the same target, so comparing across
        // agents would misreport every ingest as a "change".
        var lastRun = await db.TraceRuns
            .Where(r => r.TargetId == targetId && r.AgentId == agentId)
            .OrderByDescending(r => r.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        var destinationHop = report.Hops.LastOrDefault(h => h.Ip != null) ?? report.Hops.LastOrDefault();

        var run = new TraceRun
        {
            TargetId = targetId,
            AgentId = agentId,
            StartedAtUtc = report.StartedAtUtc,
            PacketsSent = packetsSent,
            HopCount = report.Hops.Count,
            PathHash = pathHash,
            OverallLossPct = destinationHop?.LossPct ?? 0,
            OverallAvgRttMs = destinationHop?.Avg ?? 0,
        };

        foreach (var hop in report.Hops)
        {
            run.Hops.Add(new TraceHop
            {
                HopIndex = hop.HopIndex,
                Ip = hop.Ip,
                Hostname = hop.Hostname,
                LossPct = hop.LossPct,
                Sent = hop.Sent,
                Last = hop.Last,
                Avg = hop.Avg,
                Best = hop.Best,
                Worst = hop.Worst,
                StDev = hop.StDev,
            });
        }

        var ips = report.Hops.Where(h => h.Ip != null).Select(h => h.Ip!).Distinct().ToList();
        var geo = ips.Count > 0 ? await geoIp.ResolveAsync(ips, ct) : new Dictionary<string, IpGeoCache>();
        var routeSignatureHash = ComputeRouteSignatureHash(report.Hops, geo);
        run.RouteSignatureHash = routeSignatureHash;

        db.TraceRuns.Add(run);

        var agent = await db.Agents.FindAsync([agentId], ct);

        if (lastRun is not null && lastRun.PathHash != pathHash && (agent?.PathChangeAlertsEnabled ?? true))
        {
            var previousHops = await db.TraceHops
                .Where(h => h.TraceRunId == lastRun.Id)
                .OrderBy(h => h.HopIndex)
                .ToListAsync(ct);

            db.PathChangeEvents.Add(new PathChangeEvent
            {
                TargetId = targetId,
                AgentId = agentId,
                DetectedAtUtc = DateTime.UtcNow,
                PreviousHopsJson = JsonSerializer.Serialize(previousHops.Select(h => new { h.HopIndex, h.Ip, h.Hostname }), HopSnapshotJsonOptions),
                NewHopsJson = JsonSerializer.Serialize(report.Hops.Select(h => new { h.HopIndex, h.Ip, h.Hostname }), HopSnapshotJsonOptions),
            });

            logger.LogInformation("Cambio de ruta detectado para target {TargetId} / agente {AgentId} ({Host})", targetId, agentId, report.DestinationHost);
        }

        if (routeSignatureHash is not null)
            await EnsureRouteLabelAsync(targetId, agentId, routeSignatureHash, report.Hops, geo, ct);

        if (agent is not null)
            agent.LastSeenAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return report.Hops.Where(h => h.Ip != null).Select(h => h.Ip!).ToList();
    }

    /// <summary>Hash of the ordered, deduplicated ASN sequence of responding public hops — coarser
    /// than <see cref="IPathChangeDetector.ComputePathHash"/> on purpose, so a cosmetic IP change
    /// within the same ISP (e.g. a new DHCP lease on a home gateway) doesn't count as a "new
    /// route" for labeling purposes, only an exact hop-IP change does.</summary>
    private static string? ComputeRouteSignatureHash(IReadOnlyList<MtrHopResult> hops, IReadOnlyDictionary<string, IpGeoCache> geo)
    {
        var asns = new List<string>();
        foreach (var hop in hops.OrderBy(h => h.HopIndex))
        {
            if (hop.Ip is null || !geo.TryGetValue(hop.Ip, out var g) || g.IsPrivate || string.IsNullOrWhiteSpace(g.Asn))
                continue;

            var asnNumber = g.Asn.Split(' ', 2)[0].TrimStart('A', 'S');
            if (asns.Count == 0 || asns[^1] != asnNumber)
                asns.Add(asnNumber);
        }

        if (asns.Count == 0)
            return null;

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', asns))));
    }

    /// <summary>Assigns a stable human-friendly name to a (target, agent, signature) the first
    /// time it's seen, so the route timeline can show "Cogent" or "Ruta A" instead of a hash.</summary>
    private async Task EnsureRouteLabelAsync(
        int targetId, int agentId, string routeSignatureHash, IReadOnlyList<MtrHopResult> hops,
        IReadOnlyDictionary<string, IpGeoCache> geo, CancellationToken ct)
    {
        var existing = await db.RouteLabels
            .FirstOrDefaultAsync(r => r.TargetId == targetId && r.AgentId == agentId && r.RouteSignatureHash == routeSignatureHash, ct);

        if (existing is not null)
        {
            existing.LastSeenUtc = DateTime.UtcNow;
            return;
        }

        // The last public hop before the destination is usually the ISP/transit provider's own
        // edge router — the most "identifying" hop for labeling purposes (matches how the user
        // recognizes routes today, e.g. "the Cogent path").
        var publicHops = hops.OrderBy(h => h.HopIndex)
            .Where(h => h.Ip is not null && geo.TryGetValue(h.Ip, out var g) && !g.IsPrivate)
            .ToList();
        var identifyingHop = publicHops.Count >= 2 ? publicHops[^2] : publicHops.LastOrDefault();
        geo.TryGetValue(identifyingHop?.Ip ?? "", out var identifyingGeo);
        var candidate = ExtractLabelCandidate(identifyingGeo);

        string label;
        if (candidate is null)
        {
            var existingCount = await db.RouteLabels.CountAsync(r => r.TargetId == targetId && r.AgentId == agentId, ct);
            label = existingCount < 26 ? $"Ruta {(char)('A' + existingCount)}" : $"Ruta {existingCount + 1}";
        }
        else
        {
            var collisions = await db.RouteLabels.CountAsync(r => r.TargetId == targetId && r.AgentId == agentId && r.Label == candidate, ct);
            label = collisions == 0 ? candidate : $"{candidate} ({collisions + 1})";
        }

        db.RouteLabels.Add(new RouteLabel
        {
            TargetId = targetId,
            AgentId = agentId,
            RouteSignatureHash = routeSignatureHash,
            Label = label,
            FirstSeenUtc = DateTime.UtcNow,
            LastSeenUtc = DateTime.UtcNow,
        });
    }

    private static string? ExtractLabelCandidate(IpGeoCache? geo)
    {
        if (geo is null)
            return null;
        if (!string.IsNullOrWhiteSpace(geo.Org))
            return geo.Org;
        if (!string.IsNullOrWhiteSpace(geo.Isp))
            return geo.Isp;
        if (!string.IsNullOrWhiteSpace(geo.Asn))
            return geo.Asn.Split(' ')[0];
        return null;
    }
}
