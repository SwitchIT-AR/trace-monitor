namespace TraceMonitor.Core.Models;

public class TraceRun
{
    public long Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }

    public int AgentId { get; set; }
    public Agent? Agent { get; set; }

    public DateTime StartedAtUtc { get; set; }
    public int PacketsSent { get; set; }
    public int HopCount { get; set; }

    /// <summary>Hash of the ordered, responding hop IPs — used to detect route changes cheaply.</summary>
    public required string PathHash { get; set; }

    /// <summary>Hash of the ordered, deduplicated ASN sequence of responding hops — coarser than
    /// <see cref="PathHash"/> on purpose, so a cosmetic IP change within the same ISP doesn't
    /// count as a different route. Null when no hop resolved to a public ASN yet. Groups runs
    /// into named routes via <see cref="RouteLabel"/>.</summary>
    public string? RouteSignatureHash { get; set; }

    public double OverallLossPct { get; set; }
    public double OverallAvgRttMs { get; set; }

    public List<TraceHop> Hops { get; set; } = [];
}
