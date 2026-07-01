namespace TraceMonitor.Core.Services;

public interface IPathChangeDetector
{
    /// <summary>Computes a stable hash over the responding hops' IPs, in order.</summary>
    string ComputePathHash(IReadOnlyList<MtrHopResult> hops);
}
