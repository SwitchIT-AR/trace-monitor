using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/targets")]
[Authorize]
public class TargetsController(TraceMonitorDbContext db, IUserAccessScope scope) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TargetSummaryDto>>> GetAll(CancellationToken ct)
    {
        var query = db.Targets.Where(t => t.IsActive);
        if (!scope.IsAdmin)
        {
            var allowedTargetIds = await scope.GetAllowedTargetIdsAsync(ct);
            query = query.Where(t => allowedTargetIds.Contains(t.Id));
        }

        var targets = await query.OrderBy(t => t.Name).ToListAsync(ct);
        var result = new List<TargetSummaryDto>(targets.Count);

        foreach (var target in targets)
        {
            var lastRun = await db.TraceRuns
                .Where(r => r.TargetId == target.Id)
                .OrderByDescending(r => r.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            var lastEvent = await db.PathChangeEvents
                .Where(e => e.TargetId == target.Id)
                .OrderByDescending(e => e.DetectedAtUtc)
                .FirstOrDefaultAsync(ct);

            result.Add(new TargetSummaryDto(
                target.Id,
                target.Name,
                target.Provider,
                target.DestinationHost,
                lastRun?.StartedAtUtc,
                lastRun?.OverallLossPct,
                lastRun?.OverallAvgRttMs,
                lastEvent?.DetectedAtUtc,
                target.VerifiedLat,
                target.VerifiedLon,
                target.VerifiedAddress));
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<TargetSummaryDto>> Create(CreateTargetRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.DestinationHost))
            return BadRequest("Name y DestinationHost son requeridos");

        var target = new Target
        {
            Name = request.Name,
            Provider = request.Provider,
            DestinationHost = request.DestinationHost,
            IsActive = true,
        };

        db.Targets.Add(target);
        await db.SaveChangesAsync(ct);

        return new TargetSummaryDto(target.Id, target.Name, target.Provider, target.DestinationHost, null, null, null, null, target.VerifiedLat, target.VerifiedLon, target.VerifiedAddress);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var target = await db.Targets.FindAsync([id], ct);
        if (target is null)
            return NotFound();

        target.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("{id:int}/latest")]
    public async Task<ActionResult<TraceRunDto>> GetLatest(int id, [FromServices] IGeoIpService geoIp, CancellationToken ct)
    {
        var runQuery = db.TraceRuns.Where(r => r.TargetId == id);
        if (!scope.IsAdmin)
        {
            var allowedAgentIds = await GetAllowedAgentIdsForTargetAsync(id, ct);
            runQuery = runQuery.Where(r => allowedAgentIds.Contains(r.AgentId));
        }

        var run = await runQuery
            .OrderByDescending(r => r.StartedAtUtc)
            .Include(r => r.Hops)
            .FirstOrDefaultAsync(ct);

        if (run is null)
            return NotFound();

        return await ToDtoAsync(run, geoIp, ct);
    }

    [HttpGet("{id:int}/runs")]
    public async Task<ActionResult<IReadOnlyList<RunHistoryPointDto>>> GetRuns(
        int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 500, CancellationToken ct = default)
    {
        var query = db.TraceRuns.Where(r => r.TargetId == id);
        if (!scope.IsAdmin)
        {
            var allowedAgentIds = await GetAllowedAgentIdsForTargetAsync(id, ct);
            query = query.Where(r => allowedAgentIds.Contains(r.AgentId));
        }

        if (from is not null)
            query = query.Where(r => r.StartedAtUtc >= from);
        if (to is not null)
            query = query.Where(r => r.StartedAtUtc <= to);

        var runs = await query
            .OrderByDescending(r => r.StartedAtUtc)
            .Take(limit)
            .Select(r => new RunHistoryPointDto(r.Id, r.StartedAtUtc, r.OverallLossPct, r.OverallAvgRttMs))
            .ToListAsync(ct);

        runs.Reverse();
        return runs;
    }

    /// <summary>
    /// Latest run per active agent for this target — used by the dashboard to show/compare every
    /// agent's reading instead of just whichever agent happened to report most recently overall.
    /// </summary>
    [HttpGet("{id:int}/latest-by-agent")]
    public async Task<ActionResult<IReadOnlyList<AgentTraceRunDto>>> GetLatestByAgent(int id, [FromServices] IGeoIpService geoIp, CancellationToken ct)
    {
        var target = await db.Targets.FindAsync([id], ct);
        if (target is null)
            return NotFound();

        var agents = await db.Agents.Where(a => a.IsActive).ToListAsync(ct);
        var result = new List<AgentTraceRunDto>();

        foreach (var agent in agents)
        {
            if (!await scope.CanAccessAsync(id, agent.Id, ct))
                continue;

            var run = await db.TraceRuns
                .Where(r => r.TargetId == id && r.AgentId == agent.Id)
                .OrderByDescending(r => r.StartedAtUtc)
                .Include(r => r.Hops)
                .FirstOrDefaultAsync(ct);

            if (run is null)
                continue;

            var lastEvent = await db.PathChangeEvents
                .Where(e => e.TargetId == id && e.AgentId == agent.Id)
                .OrderByDescending(e => e.DetectedAtUtc)
                .FirstOrDefaultAsync(ct);

            var hops = await ToHopDtosAsync(run.Hops, geoIp, ct);

            result.Add(new AgentTraceRunDto(
                agent.Id, agent.Name, agent.IsBuiltIn,
                run.Id, run.StartedAtUtc, run.OverallLossPct, run.OverallAvgRttMs,
                lastEvent?.DetectedAtUtc, hops));
        }

        return result;
    }

    /// <summary>
    /// Average loss per physical hop (grouped by hop index + IP, not just index, since a route
    /// change mid-window would otherwise mix loss numbers from two different hops that happened
    /// to sit at the same position) for one target+agent over the trailing window — the "which
    /// segment is dropping packets" drill-down for the dashboard's loss summary.
    /// </summary>
    [HttpGet("{id:int}/hop-loss")]
    public async Task<ActionResult<IReadOnlyList<HopLossDto>>> GetHopLoss(
        int id, [FromQuery] int agentId, [FromQuery] int hours = 24, CancellationToken ct = default)
    {
        if (!await scope.CanAccessAsync(id, agentId, ct))
            return Forbid();

        var since = DateTime.UtcNow.AddHours(-hours);

        var runIds = db.TraceRuns
            .Where(r => r.TargetId == id && r.AgentId == agentId && r.StartedAtUtc >= since)
            .Select(r => r.Id);

        // Npgsql can't translate an ordered FirstOrDefault() nested inside a GroupBy projection,
        // so the "latest hostname per (hop, ip)" lookup is a separate, plain query instead.
        var grouped = await db.TraceHops
            .Where(h => runIds.Contains(h.TraceRunId))
            .GroupBy(h => new { h.HopIndex, h.Ip })
            .Select(g => new
            {
                g.Key.HopIndex,
                g.Key.Ip,
                LatestHopId = g.Max(h => h.Id),
                AvgLossPct = g.Average(h => h.LossPct),
                SampleCount = g.Count(),
            })
            .ToListAsync(ct);

        var latestIds = grouped.Select(g => g.LatestHopId).ToList();
        var hostnamesById = await db.TraceHops
            .Where(h => latestIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, h => h.Hostname, ct);

        return grouped
            .Select(g => new HopLossDto(
                g.HopIndex, g.Ip, hostnamesById.GetValueOrDefault(g.LatestHopId), g.AvgLossPct, g.SampleCount))
            .OrderBy(d => d.HopIndex)
            .ThenByDescending(d => d.AvgLossPct)
            .ToList();
    }

    /// <summary>
    /// Collapses consecutive runs sharing the same route signature (ASN sequence) into segments —
    /// "Ruta A active for 2d4h, then Ruta B for 12m, then back to Ruta A" — instead of one row per
    /// raw run, so flapping between a couple of known ISP paths reads very differently from
    /// jumping to a new route every day. The route with the most cumulative active time in the
    /// window is marked primary; everything else is an alternate.
    /// </summary>
    [HttpGet("{id:int}/route-timeline")]
    public async Task<ActionResult<RouteTimelineDto>> GetRouteTimeline(
        int id, [FromQuery] int agentId, [FromQuery] int hours = 168, CancellationToken ct = default)
    {
        if (!await scope.CanAccessAsync(id, agentId, ct))
            return Forbid();

        var since = DateTime.UtcNow.AddHours(-hours);

        var runs = await db.TraceRuns
            .Where(r => r.TargetId == id && r.AgentId == agentId && r.StartedAtUtc >= since)
            .OrderBy(r => r.StartedAtUtc)
            .Select(r => new { r.Id, r.StartedAtUtc, r.RouteSignatureHash })
            .ToListAsync(ct);

        if (runs.Count == 0)
            return new RouteTimelineDto(0, 0, []);

        var labels = await db.RouteLabels
            .Where(l => l.TargetId == id && l.AgentId == agentId)
            .ToDictionaryAsync(l => l.RouteSignatureHash, l => l.Label, ct);

        var rawSegments = new List<(string? Hash, long RepresentativeRunId, DateTime Start, int RunCount)>();
        foreach (var run in runs)
        {
            if (rawSegments.Count > 0 && rawSegments[^1].Hash == run.RouteSignatureHash)
            {
                var last = rawSegments[^1];
                rawSegments[^1] = (last.Hash, last.RepresentativeRunId, last.Start, last.RunCount + 1);
            }
            else
            {
                rawSegments.Add((run.RouteSignatureHash, run.Id, run.StartedAtUtc, 1));
            }
        }

        // Dictionary<string, T> throws on a null key, and a good chunk of runs legitimately have
        // no RouteSignatureHash yet (no hop resolved to a public ASN) — group those under a
        // sentinel key instead of the raw nullable hash.
        const string unknownKey = "\0unknown";
        string Key(string? hash) => hash ?? unknownKey;

        var now = DateTime.UtcNow;
        var durationByHash = new Dictionary<string, TimeSpan>();
        var segmentEnds = new DateTime?[rawSegments.Count];
        for (var i = 0; i < rawSegments.Count; i++)
        {
            var end = i < rawSegments.Count - 1 ? rawSegments[i + 1].Start : (DateTime?)null;
            segmentEnds[i] = end;
            var duration = (end ?? now) - rawSegments[i].Start;
            var key = Key(rawSegments[i].Hash);
            durationByHash[key] = durationByHash.GetValueOrDefault(key) + duration;
        }

        var primaryKey = durationByHash.OrderByDescending(kv => kv.Value).First().Key;

        var segments = rawSegments.Select((s, i) => new RouteSegmentDto(
            s.Hash,
            s.Hash is not null ? labels.GetValueOrDefault(s.Hash, "Ruta desconocida") : "Ruta desconocida",
            s.RepresentativeRunId,
            s.Start,
            segmentEnds[i],
            s.RunCount,
            Key(s.Hash) == primaryKey)).ToList();

        var distinctRouteCount = rawSegments.Select(s => Key(s.Hash)).Distinct().Count();

        return new RouteTimelineDto(distinctRouteCount, segments.Count - 1, segments);
    }

    /// <summary>Hop detail for one historical run, for the route-timeline's click-to-expand view —
    /// the current segment already has this data client-side via <c>latest</c>/<c>latest-by-agent</c>.</summary>
    [HttpGet("{id:int}/runs/{runId:long}/hops")]
    public async Task<ActionResult<TraceRunDto>> GetRunHops(int id, long runId, [FromServices] IGeoIpService geoIp, CancellationToken ct)
    {
        var run = await db.TraceRuns
            .Where(r => r.Id == runId && r.TargetId == id)
            .Include(r => r.Hops)
            .FirstOrDefaultAsync(ct);

        if (run is null)
            return NotFound();

        if (!await scope.CanAccessAsync(run.TargetId, run.AgentId, ct))
            return Forbid();

        return await ToDtoAsync(run, geoIp, ct);
    }

    [HttpGet("{id:int}/events")]
    public async Task<ActionResult<IReadOnlyList<PathChangeEventDto>>> GetEvents(int id, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var eventsQuery = db.PathChangeEvents.Where(e => e.TargetId == id);
        if (!scope.IsAdmin)
        {
            var allowedAgentIds = await GetAllowedAgentIdsForTargetAsync(id, ct);
            eventsQuery = eventsQuery.Where(e => allowedAgentIds.Contains(e.AgentId));
        }

        var events = await eventsQuery
            .OrderByDescending(e => e.DetectedAtUtc)
            .Take(limit)
            .Include(e => e.Target)
            .ToListAsync(ct);

        return events.Select(e => new PathChangeEventDto(
            e.Id, e.TargetId, e.Target?.Name ?? "", e.DetectedAtUtc, e.PreviousHopsJson, e.NewHopsJson)).ToList();
    }

    private async Task<IReadOnlyList<int>> GetAllowedAgentIdsForTargetAsync(int targetId, CancellationToken ct) =>
        (await scope.GetAllowedPairsAsync(ct)).Where(p => p.TargetId == targetId).Select(p => p.AgentId).ToList();

    private async Task<TraceRunDto> ToDtoAsync(TraceRun run, IGeoIpService geoIp, CancellationToken ct)
    {
        var hops = await ToHopDtosAsync(run.Hops, geoIp, ct);
        return new TraceRunDto(run.Id, run.StartedAtUtc, run.OverallLossPct, run.OverallAvgRttMs, hops);
    }

    private static async Task<IReadOnlyList<HopDto>> ToHopDtosAsync(IEnumerable<TraceHop> hops, IGeoIpService geoIp, CancellationToken ct)
    {
        var ordered = hops.OrderBy(h => h.HopIndex).ToList();
        var ips = ordered.Where(h => h.Ip != null).Select(h => h.Ip!).ToList();
        var geo = await geoIp.ResolveAsync(ips, ct);

        return ordered.Select(h =>
        {
            geo.TryGetValue(h.Ip ?? "", out var g);
            return new HopDto(
                h.HopIndex, h.Ip, h.Hostname, h.LossPct, h.Sent, h.Last, h.Avg, h.Best, h.Worst, h.StDev,
                g?.Lat, g?.Lon, g?.City, g?.Country, g?.Asn, g?.IsPrivate ?? h.Ip is null);
        }).ToList();
    }
}
