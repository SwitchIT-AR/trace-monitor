namespace TraceMonitor.Core.Models;

/// <summary>A saved result of a manually-triggered AI network analysis (see IAiAnalysisService) —
/// persisted so past analyses stay reviewable instead of vanishing once the page is left.</summary>
public class AiAnalysisReport
{
    public long Id { get; set; }
    public required string AnalysisText { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public required string ModelUsed { get; set; }
}
