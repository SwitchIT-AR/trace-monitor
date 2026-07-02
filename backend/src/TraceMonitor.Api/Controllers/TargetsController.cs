using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/targets")]
public class TargetsController(TraceMonitorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TargetSummaryDto>>> GetAll(CancellationToken ct)
    {
        var targets = await db.Targets.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(ct);
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
        var run = await db.TraceRuns
            .Where(r => r.TargetId == id)
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

    [HttpGet("{id:int}/events")]
    public async Task<ActionResult<IReadOnlyList<PathChangeEventDto>>> GetEvents(int id, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var events = await db.PathChangeEvents
            .Where(e => e.TargetId == id)
            .OrderByDescending(e => e.DetectedAtUtc)
            .Take(limit)
            .Include(e => e.Target)
            .ToListAsync(ct);

        return events.Select(e => new PathChangeEventDto(
            e.Id, e.TargetId, e.Target?.Name ?? "", e.DetectedAtUtc, e.PreviousHopsJson, e.NewHopsJson)).ToList();
    }

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
