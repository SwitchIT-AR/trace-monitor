using TraceMonitor.Core.Models;

namespace TraceMonitor.Core.Services;

public interface IAgentAuthenticator
{
    /// <summary>Looks up the active agent owning this API key, or null if missing/unknown/inactive.</summary>
    Task<Agent?> AuthenticateAsync(string? apiKey, CancellationToken ct);
}
