using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController(TraceMonitorDbContext db, IUserAccessScope scope) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PathChangeEventDto>>> GetAll([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        // Fetch a generous window first, then filter and trim to `limit` — filtering by allowed
        // pairs after Take(limit) could otherwise leave a non-admin with fewer than `limit` rows
        // even when more visible events exist further back.
        var candidates = await db.PathChangeEvents
            .OrderByDescending(e => e.DetectedAtUtc)
            .Take(scope.IsAdmin ? limit : limit * 10)
            .Include(e => e.Target)
            .ToListAsync(ct);

        if (!scope.IsAdmin)
        {
            var allowedPairs = (await scope.GetAllowedPairsAsync(ct)).ToHashSet();
            candidates = candidates.Where(e => allowedPairs.Contains((e.TargetId, e.AgentId))).Take(limit).ToList();
        }

        return candidates.Select(e => new PathChangeEventDto(
            e.Id, e.TargetId, e.Target?.Name ?? "", e.DetectedAtUtc, e.PreviousHopsJson, e.NewHopsJson)).ToList();
    }
}
