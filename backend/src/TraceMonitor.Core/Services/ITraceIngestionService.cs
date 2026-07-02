namespace TraceMonitor.Core.Services;

public interface ITraceIngestionService
{
    /// <summary>
    /// Persists one mtr report as a <see cref="Models.TraceRun"/> for the given (target, agent)
    /// pair, detects a route change against that pair's previous run, and updates the agent's
    /// last-seen timestamp. Returns the responding hops' IPs, for GeoIP resolution.
    /// </summary>
    Task<IReadOnlyList<string>> IngestAsync(int agentId, int targetId, int packetsSent, MtrReport report, CancellationToken ct);
}
