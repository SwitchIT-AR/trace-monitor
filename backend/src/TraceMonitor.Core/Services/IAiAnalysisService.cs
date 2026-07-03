namespace TraceMonitor.Core.Services;

public record AiAnalysisResult(long Id, string AnalysisText, DateTime GeneratedAtUtc, string ModelUsed);

public interface IAiAnalysisService
{
    Task<AiAnalysisResult> AnalyzeAsync(CancellationToken ct);
}
