namespace TraceMonitor.Api.Contracts;

public record TargetSummaryDto(
    int Id,
    string Name,
    string Provider,
    string DestinationHost,
    DateTime? LastRunAtUtc,
    double? LastLossPct,
    double? LastAvgRttMs,
    DateTime? LastPathChangeAtUtc);

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
