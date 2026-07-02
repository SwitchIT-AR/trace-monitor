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

        db.TraceRuns.Add(run);

        if (lastRun is not null && lastRun.PathHash != pathHash)
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

        var agent = await db.Agents.FindAsync([agentId], ct);
        if (agent is not null)
            agent.LastSeenAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return report.Hops.Where(h => h.Ip != null).Select(h => h.Ip!).ToList();
    }
}
