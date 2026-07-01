namespace TraceMonitor.Core.Models;

public class TraceRun
{
    public long Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public int PacketsSent { get; set; }
    public int HopCount { get; set; }

    /// <summary>Hash of the ordered, responding hop IPs — used to detect route changes cheaply.</summary>
    public required string PathHash { get; set; }

    public double OverallLossPct { get; set; }
    public double OverallAvgRttMs { get; set; }

    public List<TraceHop> Hops { get; set; } = [];
}
