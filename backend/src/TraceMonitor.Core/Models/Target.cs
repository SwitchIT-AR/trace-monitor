namespace TraceMonitor.Core.Models;

public class Target
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Provider { get; set; }
    public required string DestinationHost { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<TraceRun> Runs { get; set; } = [];
    public List<PathChangeEvent> Events { get; set; } = [];
}
