using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Security;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController(TraceMonitorDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentSummaryDto>>> GetAll(CancellationToken ct)
    {
        var agents = await db.Agents.OrderBy(a => a.Name).ToListAsync(ct);
        return agents.Select(ToSummaryDto).ToList();
    }

    /// <summary>
    /// Creates a new agent and returns its plaintext API key — the only time it is ever
    /// returned. Only the key's hash is persisted, so lose it and you have to issue a new one.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AgentCreatedDto>> Create(CreateAgentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location) || string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest("Name, Location y Provider son requeridos");

        var apiKey = ApiKeyUtil.GenerateKey();
        var agent = new Agent
        {
            Name = request.Name,
            Location = request.Location,
            Provider = request.Provider,
            ApiKeyHash = ApiKeyUtil.Hash(apiKey),
            IsActive = true,
        };

        db.Agents.Add(agent);
        await db.SaveChangesAsync(ct);

        return new AgentCreatedDto(agent.Id, agent.Name, apiKey);
    }

    /// <summary>
    /// Edits an existing agent's Name/Location/Provider and/or its map-origin marker
    /// (Lat/Lon/Address — used to fix an imprecise IP-based auto-location, see
    /// IngestController.LocateAgentIfUnknownAsync, or to backfill one that hasn't reported yet).
    /// Does not touch the API key, so already-deployed remote agents keep working unchanged.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAgentRequest request, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([id], ct);
        if (agent is null)
            return NotFound();

        if (agent.IsBuiltIn)
            return BadRequest("El agente built-in de oficina se configura por Office:* en appsettings, no por API");

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location) || string.IsNullOrWhiteSpace(request.Provider))
            return BadRequest("Name, Location y Provider son requeridos");

        if ((request.Lat is null) != (request.Lon is null))
            return BadRequest("Lat y Lon deben especificarse juntos");

        agent.Name = request.Name;
        agent.Location = request.Location;
        agent.Provider = request.Provider;
        agent.Lat = request.Lat;
        agent.Lon = request.Lon;
        agent.Address = request.Address;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        var agent = await db.Agents.FindAsync([id], ct);
        if (agent is null)
            return NotFound();

        if (agent.IsBuiltIn)
            return BadRequest("No se puede desactivar el agente built-in de la oficina");

        agent.IsActive = false;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static AgentSummaryDto ToSummaryDto(Agent a) =>
        new(a.Id, a.Name, a.Location, a.Provider, a.IsActive, a.IsBuiltIn, a.CreatedAtUtc, a.LastSeenAtUtc, a.Lat, a.Lon, a.Address);
}
