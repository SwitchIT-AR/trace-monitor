using Microsoft.EntityFrameworkCore;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Security;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Services;

public class AgentAuthenticator(TraceMonitorDbContext db) : IAgentAuthenticator
{
    public async Task<Agent?> AuthenticateAsync(string? apiKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var hash = ApiKeyUtil.Hash(apiKey);
        return await db.Agents.FirstOrDefaultAsync(a => a.ApiKeyHash == hash && a.IsActive, ct);
    }
}
