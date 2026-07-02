using System.Net.Http.Json;
using System.Text.Json;
using TraceMonitor.Core.Services;

namespace TraceMonitor.Agent;

/// <summary>
/// Every `Trace:IntervalSeconds` (default 2 minutes), fetches the active target list from the
/// central API and runs a short mtr report burst against each one, in parallel, then pushes
/// each result back to `POST /api/ingest/traces` authenticated with this agent's API key. Mirrors
/// TraceMonitor.Infrastructure's TraceSchedulerWorker, but reports over HTTP instead of writing
/// to the database directly, since this process runs at a remote site.
/// </summary>
public class AgentWorker(
    IHttpClientFactory httpClientFactory,
    IMtrRunner mtrRunner,
    IConfiguration config,
    ILogger<AgentWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        var client = httpClientFactory.CreateClient("central-api");

        var targets = await client.GetFromJsonAsync<List<TargetDto>>("api/targets", JsonOptions, ct) ?? [];
        if (targets.Count == 0)
        {
            logger.LogWarning("El backend central no devolvio ningun target activo");
            return;
        }

        await Task.WhenAll(targets.Select(t => ProcessTargetAsync(client, t, cycles, ct)));
    }

    private async Task ProcessTargetAsync(HttpClient client, TargetDto target, int cycles, CancellationToken ct)
    {
        try
        {
            var report = await mtrRunner.RunAsync(target.DestinationHost, cycles, ct);

            var request = new IngestTraceRequest(
                target.Id,
                report.StartedAtUtc,
                cycles,
                report.Hops
                    .Select(h => new IngestHopDto(h.HopIndex, h.Ip, h.Hostname, h.LossPct, h.Sent, h.Last, h.Avg, h.Best, h.Worst, h.StDev))
                    .ToList());

            var response = await client.PostAsJsonAsync("api/ingest/traces", request, JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "El backend central rechazo la traza hacia {Host} ({Status}): {Body}",
                    target.DestinationHost, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo la traza hacia {Host} (target {TargetId})", target.DestinationHost, target.Id);
        }
    }
}
