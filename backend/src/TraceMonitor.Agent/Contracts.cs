namespace TraceMonitor.Agent;

/// <summary>Subset of TraceMonitor.Api's TargetSummaryDto — only the fields this agent needs to run mtr.</summary>
public record TargetDto(int Id, string Name, string Provider, string DestinationHost);

public record IngestHopDto(
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

public record IngestTraceRequest(
    int TargetId,
    DateTime StartedAtUtc,
    int PacketsSent,
    IReadOnlyList<IngestHopDto> Hops);
