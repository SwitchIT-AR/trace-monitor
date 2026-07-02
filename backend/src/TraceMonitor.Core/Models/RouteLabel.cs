namespace TraceMonitor.Core.Models;

/// <summary>
/// Gives a stable human-friendly name to a (target, agent, route-signature) triple the first
/// time it's seen, so recurring routes are recognizable across the route timeline instead of
/// being silently renamed if the same path reappears weeks later.
/// </summary>
public class RouteLabel
{
    public long Id { get; set; }
    public int TargetId { get; set; }
    public Target? Target { get; set; }

    public int AgentId { get; set; }
    public Agent? Agent { get; set; }

    public required string RouteSignatureHash { get; set; }

    /// <summary>Auto-derived from the ISP/org of the most identifying hop (e.g. "Cogent"), or
    /// "Ruta A"/"Ruta B"/... when no ASN/org data was available for that signature.</summary>
    public required string Label { get; set; }

    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
}
