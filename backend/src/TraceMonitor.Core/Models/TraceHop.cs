namespace TraceMonitor.Core.Models;

public class TraceHop
{
    public long Id { get; set; }
    public long TraceRunId { get; set; }
    public TraceRun? TraceRun { get; set; }

    public int HopIndex { get; set; }
    public string? Ip { get; set; }
    public string? Hostname { get; set; }

    public double LossPct { get; set; }
    public int Sent { get; set; }
    public double Last { get; set; }
    public double Avg { get; set; }
    public double Best { get; set; }
    public double Worst { get; set; }
    public double StDev { get; set; }
}
