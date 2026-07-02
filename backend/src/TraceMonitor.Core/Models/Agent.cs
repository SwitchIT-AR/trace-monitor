namespace TraceMonitor.Core.Models;

/// <summary>
/// A probe that runs `mtr` from a physical location (office, ISP POP, remote site, etc.) and
/// reports its runs to this backend. One built-in agent (<see cref="IsBuiltIn"/>) represents the
/// office origin and is fed in-process by <c>TraceSchedulerWorker</c>; every other agent is an
/// external <c>TraceMonitor.Agent</c> instance authenticating with <see cref="ApiKeyHash"/>.
/// </summary>
public class Agent
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Location { get; set; }
    public required string Provider { get; set; }

    /// <summary>SHA-256 hash of the agent's API key; the plaintext key is only ever shown once, at creation.</summary>
    public required string ApiKeyHash { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>True only for the seeded office agent fed in-process — it has no API key auth.</summary>
    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAtUtc { get; set; }

    public double? Lat { get; set; }
    public double? Lon { get; set; }
    public string? Address { get; set; }

    public List<TraceRun> Runs { get; set; } = [];
    public List<PathChangeEvent> Events { get; set; } = [];
}
