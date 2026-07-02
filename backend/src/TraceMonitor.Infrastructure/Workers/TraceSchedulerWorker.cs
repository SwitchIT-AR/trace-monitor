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
/// every active target, in parallel, and persists the snapshot under the built-in "Oficina"
/// agent (<see cref="TraceMonitorDbContext.BuiltInAgentId"/>). Each burst is a self-contained
/// process invocation (see <see cref="IMtrRunner"/>) rather than a long-lived mtr session, so a
/// container restart never leaves anything half-finished.
/// </summary>
public class TraceSchedulerWorker(
    IServiceScopeFactory scopeFactory,
    IMtrRunner mtrRunner,
    IConfiguration config,
    ILogger<TraceSchedulerWorker> logger) : BackgroundService
{
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
        var ingestion = scope.ServiceProvider.GetRequiredService<ITraceIngestionService>();

        try
        {
            var report = await mtrRunner.RunAsync(destinationHost, cycles, ct);
            return await ingestion.IngestAsync(TraceMonitorDbContext.BuiltInAgentId, targetId, cycles, report, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo la traza hacia {Host} (target {TargetId})", destinationHost, targetId);
            return [];
        }
    }
}
