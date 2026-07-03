using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Services;

public class UserAccessScope(TraceMonitorDbContext db, IHttpContextAccessor httpContextAccessor) : IUserAccessScope
{
    public bool IsAdmin => httpContextAccessor.HttpContext?.User.FindFirstValue("isAdmin") == "True";

    private int? UserId
    {
        get
        {
            var idClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return idClaim is not null ? int.Parse(idClaim) : null;
        }
    }

    public async Task<bool> CanAccessAsync(int targetId, int agentId, CancellationToken ct)
    {
        if (IsAdmin)
            return true;
        if (UserId is not { } uid)
            return false;

        return await db.UserTargetAgentAccess.AnyAsync(
            a => a.UserId == uid && a.TargetId == targetId && a.AgentId == agentId, ct);
    }

    public async Task<IReadOnlyList<(int TargetId, int AgentId)>> GetAllowedPairsAsync(CancellationToken ct)
    {
        if (IsAdmin || UserId is not { } uid)
            return [];

        var pairs = await db.UserTargetAgentAccess
            .Where(a => a.UserId == uid)
            .Select(a => new { a.TargetId, a.AgentId })
            .ToListAsync(ct);

        return pairs.Select(p => (p.TargetId, p.AgentId)).ToList();
    }

    public async Task<IReadOnlyList<int>> GetAllowedTargetIdsAsync(CancellationToken ct) =>
        (await GetAllowedPairsAsync(ct)).Select(p => p.TargetId).Distinct().ToList();

    public async Task<IReadOnlyList<int>> GetAllowedAgentIdsAsync(CancellationToken ct) =>
        (await GetAllowedPairsAsync(ct)).Select(p => p.AgentId).Distinct().ToList();
}
