namespace TraceMonitor.Core.Services;

public record MtrHopResult(
    int HopIndex,
    string? Ip,
    string? Hostname,
    double LossPct,
    int Sent,
    double Last,
    double Avg,
    double Best,
    double Worst,
    double StDev);

public record MtrReport(
    string DestinationHost,
    DateTime StartedAtUtc,
    IReadOnlyList<MtrHopResult> Hops);

public interface IMtrRunner
{
    Task<MtrReport> RunAsync(string destinationHost, int reportCycles, CancellationToken ct);
}
