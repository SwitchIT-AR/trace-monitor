namespace TraceMonitor.Core.Models;

public class Target
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Provider { get; set; }
    public required string DestinationHost { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Manually verified real-world address of this destination (looked up from the
    /// business's actual known address, not inferred) — used because IP geolocation for the
    /// final hop often only resolves to the ISP's node/POP, not the customer's real location.</summary>
    public double? VerifiedLat { get; set; }
    public double? VerifiedLon { get; set; }
    public string? VerifiedAddress { get; set; }

    public List<TraceRun> Runs { get; set; } = [];
    public List<PathChangeEvent> Events { get; set; } = [];
}
