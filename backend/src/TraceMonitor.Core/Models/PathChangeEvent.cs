namespace TraceMonitor.Core.Models;

public class PathChangeEvent
{
    public long Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }

    public DateTime DetectedAtUtc { get; set; }

    /// <summary>JSON snapshot of the previous run's hops (ip/hostname per hop index).</summary>
    public required string PreviousHopsJson { get; set; }

    /// <summary>JSON snapshot of the new run's hops (ip/hostname per hop index).</summary>
    public required string NewHopsJson { get; set; }
}
