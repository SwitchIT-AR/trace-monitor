using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(TraceMonitorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PathChangeEventDto>>> GetAll([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var events = await db.PathChangeEvents
            .OrderByDescending(e => e.DetectedAtUtc)
            .Take(limit)
            .Include(e => e.Target)
            .ToListAsync(ct);

        return events.Select(e => new PathChangeEventDto(
            e.Id, e.TargetId, e.Target?.Name ?? "", e.DetectedAtUtc, e.PreviousHopsJson, e.NewHopsJson)).ToList();
    }
}
