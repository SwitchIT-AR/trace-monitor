namespace TraceMonitor.Core.Services;

/// <summary>Resolves the current request's user and their allowed (Target, Agent) pairs. Admins
/// bypass every check here — callers should treat IsAdmin as "no filtering needed" rather than
/// calling the Get*Allowed* methods for admins (those return an empty list for admins on purpose,
/// since an admin's visibility isn't stored as explicit grants).</summary>
public interface IUserAccessScope
{
    bool IsAdmin { get; }
    Task<bool> CanAccessAsync(int targetId, int agentId, CancellationToken ct);
    Task<IReadOnlyList<(int TargetId, int AgentId)>> GetAllowedPairsAsync(CancellationToken ct);
    Task<IReadOnlyList<int>> GetAllowedTargetIdsAsync(CancellationToken ct);
    Task<IReadOnlyList<int>> GetAllowedAgentIdsAsync(CancellationToken ct);
}
