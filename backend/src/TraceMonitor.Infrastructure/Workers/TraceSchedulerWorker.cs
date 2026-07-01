using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Workers;

/// <summary>
/// Every `Trace:IntervalSeconds` (default 2 minutes), runs a short mtr report burst against
/// every active target, in parallel, and persists the snapshot. Each burst is a self-contained
/// process invocation (see <see cref="IMtrRunner"/>) rather than a long-lived mtr session, so a
/// container restart never leaves anything half-finished.
/// </summary>
public class TraceSchedulerWorker(
    IServiceScopeFactory scopeFactory,
    IMtrRunner mtrRunner,
    IPathChangeDetector detector,
    IConfiguration config,
    ILogger<TraceSchedulerWorker> logger) : BackgroundService
{
    // The frontend consumes PreviousHopsJson/NewHopsJson directly (they're opaque strings to the
    // API layer, so ASP.NET Core's own camelCase policy never touches them) — serialize with the
    // same casing so both sides agree on the hop snapshot shape.
    private static readonly JsonSerializerOptions HopSnapshotJsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(config.GetValue("Trace:IntervalSeconds", 120));
        var cycles = config.GetValue("Trace:ReportCycles", 10);

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await RunCycleAsync(cycles, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error corriendo el ciclo de trazas");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunCycleAsync(int cycles, CancellationToken ct)
    {
        List<Target> targets;
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TraceMonitorDbContext>();
            targets = await db.Targets.Where(t => t.IsActive).AsNoTracking().ToListAsync(ct);
        }

        var ipsPerTarget = await Task.WhenAll(targets.Select(t => ProcessTargetAsync(t.Id, t.DestinationHost, cycles, ct)));

        // Resolved once, after every target's run is saved: several targets share the same
        // early hops (office gateway, first ISP hop), so resolving concurrently per-target
        // raced on inserting the same IpGeoCache row.
        var allIps = ipsPerTarget.SelectMany(ips => ips).Distinct().ToList();
        if (allIps.Count > 0)
        {
            using var scope = scopeFactory.CreateScope();
            var geoIp = scope.ServiceProvider.GetRequiredService<IGeoIpService>();
            await geoIp.ResolveAsync(allIps, ct);
        }
    }

    private async Task<IReadOnlyList<string>> ProcessTargetAsync(int targetId, string destinationHost, int cycles, CancellationToken ct)
    {
        // Each parallel target gets its own scope/DbContext — DbContext isn't thread-safe.
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TraceMonitorDbContext>();

        try
        {
            var report = await mtrRunner.RunAsync(destinationHost, cycles, ct);
            var pathHash = detector.ComputePathHash(report.Hops);

            var lastRun = await db.TraceRuns
                .Where(r => r.TargetId == targetId)
                .OrderByDescending(r => r.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            var destinationHop = report.Hops.LastOrDefault(h => h.Ip != null) ?? report.Hops.LastOrDefault();

            var run = new TraceRun
            {
                TargetId = targetId,
                StartedAtUtc = report.StartedAtUtc,
                PacketsSent = cycles,
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
                    DetectedAtUtc = DateTime.UtcNow,
                    PreviousHopsJson = JsonSerializer.Serialize(previousHops.Select(h => new { h.HopIndex, h.Ip, h.Hostname }), HopSnapshotJsonOptions),
                    NewHopsJson = JsonSerializer.Serialize(report.Hops.Select(h => new { h.HopIndex, h.Ip, h.Hostname }), HopSnapshotJsonOptions),
                });

                logger.LogInformation("Cambio de ruta detectado para target {TargetId} ({Host})", targetId, destinationHost);
            }

            await db.SaveChangesAsync(ct);

            return report.Hops.Where(h => h.Ip != null).Select(h => h.Ip!).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo la traza hacia {Host} (target {TargetId})", destinationHost, targetId);
            return [];
        }
    }
}
