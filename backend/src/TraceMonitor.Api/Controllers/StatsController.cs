using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(TraceMonitorDbContext db, IUserAccessScope scope) : ControllerBase
{
    /// <summary>Average loss per (target, agent) pair over the trailing window, worst first —
    /// the "which route is having problems" summary for the dashboard.</summary>
    [HttpGet("loss-summary")]
    public async Task<ActionResult<IReadOnlyList<LossSummaryDto>>> GetLossSummary([FromQuery] int hours = 24, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var grouped = await db.TraceRuns
            .Where(r => r.StartedAtUtc >= since)
            .GroupBy(r => new { r.TargetId, r.AgentId })
            .Select(g => new
            {
                g.Key.TargetId,
                g.Key.AgentId,
                AvgLossPct = g.Average(r => r.OverallLossPct),
                RunCount = g.Count(),
            })
            .ToListAsync(ct);

        if (grouped.Count == 0)
            return new List<LossSummaryDto>();

        // Resolved separately (not via navigation properties in the GroupBy above) — EF can't
        // translate a Include()'d navigation through a GroupBy/Select projection cleanly, and
        // these lookup sets are small (targets/agents, not runs).
        var targetNames = await db.Targets.ToDictionaryAsync(t => t.Id, t => t.Name, ct);
        var agentNames = await db.Agents.ToDictionaryAsync(a => a.Id, a => a.Name, ct);

        var filtered = grouped.Where(g => targetNames.ContainsKey(g.TargetId) && agentNames.ContainsKey(g.AgentId));

        if (!scope.IsAdmin)
        {
            var allowedPairs = (await scope.GetAllowedPairsAsync(ct)).ToHashSet();
            filtered = filtered.Where(g => allowedPairs.Contains((g.TargetId, g.AgentId)));
        }

        return filtered
            .Select(g => new LossSummaryDto(
                g.TargetId, targetNames[g.TargetId],
                g.AgentId, agentNames[g.AgentId],
                g.AvgLossPct, g.RunCount))
            .OrderByDescending(d => d.AvgLossPct)
            .ToList();
    }
}
