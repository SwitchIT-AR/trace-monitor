namespace TraceMonitor.Api.Contracts;

public record TargetSummaryDto(
    int Id,
    string Name,
    string Provider,
    string DestinationHost,
    DateTime? LastRunAtUtc,
    double? LastLossPct,
    double? LastAvgRttMs,
    DateTime? LastPathChangeAtUtc,
    double? VerifiedLat,
    double? VerifiedLon,
    string? VerifiedAddress);

public record OfficeLocationDto(double Lat, double Lon, string Address);

public record HopDto(
    int HopIndex,
    string? Ip,
    string? Hostname,
    double LossPct,
    int Sent,
    double Last,
    double Avg,
    double Best,
    double Worst,
    double StDev,
    double? Lat,
    double? Lon,
    string? City,
    string? Country,
    string? Asn,
    bool IsPrivate);

public record TraceRunDto(
    long Id,
    DateTime StartedAtUtc,
    double OverallLossPct,
    double OverallAvgRttMs,
    IReadOnlyList<HopDto> Hops);

public record RunHistoryPointDto(
    long Id,
    DateTime StartedAtUtc,
    double OverallLossPct,
    double OverallAvgRttMs);

public record PathChangeEventDto(
    long Id,
    int TargetId,
    string TargetName,
    DateTime DetectedAtUtc,
    string PreviousHopsJson,
    string NewHopsJson);

public record CreateTargetRequest(string Name, string Provider, string DestinationHost);

public record AgentSummaryDto(
    int Id,
    string Name,
    string Location,
    string Provider,
    bool IsActive,
    bool IsBuiltIn,
    DateTime CreatedAtUtc,
    DateTime? LastSeenAtUtc,
    double? Lat,
    double? Lon,
    string? Address);

/// <summary>Returned only once, right after creation — the plaintext ApiKey is never retrievable again.</summary>
public record AgentCreatedDto(int Id, string Name, string ApiKey);

public record CreateAgentRequest(string Name, string Location, string Provider);

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
