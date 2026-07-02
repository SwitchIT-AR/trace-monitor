using System.Security.Cryptography;
using System.Text;

namespace TraceMonitor.Core.Security;

/// <summary>
/// Agent API keys are shown to the user exactly once, at creation — only their SHA-256 hash is
/// persisted, so a database leak alone doesn't let an attacker impersonate an agent.
/// </summary>
public static class ApiKeyUtil
{
    public static string GenerateKey() => "tma_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    public static string Hash(string apiKey) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey))).ToLowerInvariant();
}
