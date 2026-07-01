using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TraceMonitor.Core.Services;

namespace TraceMonitor.Infrastructure.Services;

/// <summary>
/// Runs `mtr` in short report bursts (not a long-lived interactive session) and parses its
/// JSON output into a clean snapshot. Each call is a self-contained process invocation, so a
/// container restart never leaves a stuck mtr process behind.
/// </summary>
public partial class MtrRunner(ILogger<MtrRunner> logger) : IMtrRunner
{
    public async Task<MtrReport> RunAsync(string destinationHost, int reportCycles, CancellationToken ct)
    {
        var startedAt = DateTime.UtcNow;

        var psi = new ProcessStartInfo
        {
            FileName = "mtr",
            // mtr's display-mode flags are last-one-wins, not additive — "-j" must come after
            // any other mode flag (there is none here, but keep it last to stay safe).
            ArgumentList = { "-4", "-b", "-c", reportCycles.ToString(), "-j", destinationHost },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"No se pudo iniciar el proceso mtr para {destinationHost}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
        {
            logger.LogWarning("mtr hacia {Host} termino con codigo {Code}: {Error}", destinationHost, process.ExitCode, stderr);
            throw new InvalidOperationException($"mtr hacia {destinationHost} fallo (codigo {process.ExitCode}): {stderr}");
        }

        var parsed = JsonSerializer.Deserialize<MtrJsonRoot>(stdout, JsonOptions)
            ?? throw new InvalidOperationException($"No se pudo parsear la salida JSON de mtr para {destinationHost}");

        var hubs = parsed.Report?.Hubs ?? [];
        var hops = new List<MtrHopResult>(hubs.Count);

        foreach (var hub in hubs)
        {
            var (hostname, ip) = SplitHostAndIp(hub.Host);
            hops.Add(new MtrHopResult(
                HopIndex: hub.Count,
                Ip: ip,
                Hostname: hostname,
                LossPct: hub.LossPct,
                Sent: hub.Snt,
                Last: hub.Last,
                Avg: hub.Avg,
                Best: hub.Best,
                Worst: hub.Wrst,
                StDev: hub.StDev));
        }

        return new MtrReport(destinationHost, startedAt, hops);
    }

    /// <summary>mtr -b prints "hostname (ip)" when a PTR record resolves, or just "ip" / "???" otherwise.</summary>
    private static (string? Hostname, string? Ip) SplitHostAndIp(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "???")
            return (null, null);

        var match = HostIpRegex().Match(host);
        if (match.Success)
        {
            var hostname = match.Groups["host"].Value;
            var ip = match.Groups["ip"].Value;
            return (hostname == ip ? null : hostname, ip);
        }

        return (null, host);
    }

    [GeneratedRegex(@"^(?<host>\S+)\s+\((?<ip>[0-9a-fA-F:.]+)\)$")]
    private static partial Regex HostIpRegex();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private class MtrJsonRoot
    {
        [JsonPropertyName("report")]
        public MtrJsonReport? Report { get; set; }
    }

    private class MtrJsonReport
    {
        [JsonPropertyName("hubs")]
        public List<MtrJsonHub>? Hubs { get; set; }
    }

    private class MtrJsonHub
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("host")]
        public string? Host { get; set; }

        [JsonPropertyName("Loss%")]
        public double LossPct { get; set; }

        [JsonPropertyName("Snt")]
        public int Snt { get; set; }

        [JsonPropertyName("Last")]
        public double Last { get; set; }

        [JsonPropertyName("Avg")]
        public double Avg { get; set; }

        [JsonPropertyName("Best")]
        public double Best { get; set; }

        [JsonPropertyName("Wrst")]
        public double Wrst { get; set; }

        [JsonPropertyName("StDev")]
        public double StDev { get; set; }
    }
}
