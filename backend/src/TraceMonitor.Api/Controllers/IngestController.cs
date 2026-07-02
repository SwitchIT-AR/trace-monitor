using Microsoft.AspNetCore.Mvc;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

/// <summary>
/// Receives trace runs pushed by remote <c>TraceMonitor.Agent</c> instances, authenticated via
/// the `X-Agent-Key` header (see <see cref="IAgentAuthenticator"/>). The in-process office
/// worker doesn't call this — it uses <see cref="ITraceIngestionService"/> directly.
/// </summary>
[ApiController]
[Route("api/ingest")]
public class IngestController(
    TraceMonitorDbContext db,
    IAgentAuthenticator authenticator,
    ITraceIngestionService ingestion,
    IGeoIpService geoIp) : ControllerBase
{
    [HttpPost("traces")]
    public async Task<IActionResult> PostTrace(IngestTraceRequest request, CancellationToken ct)
    {
        var apiKey = Request.Headers["X-Agent-Key"].FirstOrDefault();
        var agent = await authenticator.AuthenticateAsync(apiKey, ct);
        if (agent is null)
            return Unauthorized();

        var target = await db.Targets.FindAsync([request.TargetId], ct);
        if (target is null || !target.IsActive)
            return NotFound("Target no encontrado o inactivo");

        if (request.Hops.Count == 0)
            return BadRequest("Hops no puede estar vacio");

        var hops = request.Hops
            .Select(h => new MtrHopResult(h.HopIndex, h.Ip, h.Hostname, h.LossPct, h.Sent, h.Last, h.Avg, h.Best, h.Worst, h.StDev))
            .ToList();
        var report = new MtrReport(target.DestinationHost, request.StartedAtUtc, hops);

        var ips = await ingestion.IngestAsync(agent.Id, target.Id, request.PacketsSent, report, ct);
        if (ips.Count > 0)
            await geoIp.ResolveAsync(ips, ct);

        return NoContent();
    }
}
