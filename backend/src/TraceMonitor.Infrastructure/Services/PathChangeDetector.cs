using System.Security.Cryptography;
using System.Text;
using TraceMonitor.Core.Services;

namespace TraceMonitor.Infrastructure.Services;

public class PathChangeDetector : IPathChangeDetector
{
    public string ComputePathHash(IReadOnlyList<MtrHopResult> hops)
    {
        // Only responding hops define "the path" — a transient timeout on one hop of an
        // otherwise identical route shouldn't be flagged as a route change.
        var sb = new StringBuilder();
        foreach (var hop in hops)
        {
            if (hop.Ip is null)
                continue;

            sb.Append(hop.HopIndex).Append(':').Append(hop.Ip).Append('|');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
