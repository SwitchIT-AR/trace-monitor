using Microsoft.AspNetCore.Mvc;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Models;
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

        await LocateAgentIfUnknownAsync(agent, ct);

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

    /// <summary>
    /// First successful report from a remote agent with no location yet: geolocate the caller's
    /// own IP (same GeoIP path used for hops) and use it as an approximate origin marker. Runs
    /// once — once <see cref="Agent.Lat"/> is set (auto or manually corrected via
    /// PUT /api/agents/{id}/location) this never overwrites it again. nginx always overwrites
    /// X-Real-IP with the real connecting IP (see frontend/nginx.conf), so this isn't spoofable
    /// by the agent itself.
    /// </summary>
    private async Task LocateAgentIfUnknownAsync(Agent agent, CancellationToken ct)
    {
        if (agent.IsBuiltIn || agent.Lat is not null)
            return;

        var clientIp = Request.Headers["X-Real-IP"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
        if (clientIp is null)
            return;

        var geo = await geoIp.ResolveAsync([clientIp], ct);
        if (!geo.TryGetValue(clientIp, out var g) || g.IsPrivate || g.Lat is null || g.Lon is null)
            return;

        agent.Lat = g.Lat;
        agent.Lon = g.Lon;
        agent.Address = $"{g.City}, {g.Country} (ubicación aproximada por IP)";
        await db.SaveChangesAsync(ct);
    }
}
