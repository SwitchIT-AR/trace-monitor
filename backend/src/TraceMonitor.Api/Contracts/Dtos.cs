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
    string? Address,
    bool PathChangeAlertsEnabled);

/// <summary>Returned only once, right after creation — the plaintext ApiKey is never retrievable again.</summary>
public record AgentCreatedDto(int Id, string Name, string ApiKey);

public record CreateAgentRequest(string Name, string Location, string Provider);

/// <summary>Full edit of an existing agent's Name/Location/Provider plus its map-origin marker.
/// Lat/Lon override the automatic IP-based geolocation (see IngestController), which only
/// approximates city level; send Lat/Lon/Address all null to clear the marker.</summary>
public record UpdateAgentRequest(
    string Name, string Location, string Provider, double? Lat, double? Lon, string? Address, bool PathChangeAlertsEnabled);

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

public record AgentTraceRunDto(
    int AgentId,
    string AgentName,
    bool AgentIsBuiltIn,
    long RunId,
    DateTime StartedAtUtc,
    double OverallLossPct,
    double OverallAvgRttMs,
    DateTime? LastPathChangeAtUtc,
    IReadOnlyList<HopDto> Hops);

public record LossSummaryDto(
    int TargetId,
    string TargetName,
    int AgentId,
    string AgentName,
    double AvgLossPct,
    int RunCount);

public record HopLossDto(
    int HopIndex,
    string? Ip,
    string? Hostname,
    double AvgLossPct,
    int SampleCount);

public record RouteSegmentDto(
    string? RouteSignatureHash,
    string Label,
    long RepresentativeRunId,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int RunCount,
    bool IsPrimary);

public record RouteTimelineDto(
    int DistinctRouteCount,
    int FlapCount,
    IReadOnlyList<RouteSegmentDto> Segments);

public record AiAnalysisResultDto(long Id, string AnalysisText, DateTime GeneratedAtUtc, string ModelUsed);

public record AiAnalysisReportSummaryDto(long Id, DateTime GeneratedAtUtc, string ModelUsed);

public record LoginRequest(string Username, string Password);

public record MeDto(int Id, string Username, bool IsAdmin);

public record MaskedKeyDto(string? Masked);

public record SetKeyRequest(string Value);

public record UserSummaryDto(int Id, string Username, bool IsActive, bool IsAdmin, DateTime CreatedAtUtc);

public record CreateUserRequest(string Username, string Password, bool IsAdmin);

public record ResetPasswordRequest(string NewPassword);

public record AccessPairDto(int TargetId, int AgentId);
