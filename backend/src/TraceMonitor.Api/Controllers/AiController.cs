using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize(Policy = "AdminOnly")]
public class AiController(IAiAnalysisService aiAnalysis, TraceMonitorDbContext db) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<ActionResult<AiAnalysisResultDto>> Analyze(CancellationToken ct)
    {
        var result = await aiAnalysis.AnalyzeAsync(ct);
        return new AiAnalysisResultDto(result.Id, result.AnalysisText, result.GeneratedAtUtc, result.ModelUsed);
    }

    [HttpGet("reports")]
    public async Task<ActionResult<IReadOnlyList<AiAnalysisReportSummaryDto>>> GetReports([FromQuery] int limit = 20, CancellationToken ct = default) =>
        await db.AiAnalysisReports
            .OrderByDescending(r => r.GeneratedAtUtc)
            .Take(limit)
            .Select(r => new AiAnalysisReportSummaryDto(r.Id, r.GeneratedAtUtc, r.ModelUsed))
            .ToListAsync(ct);

    [HttpGet("reports/{id:long}")]
    public async Task<ActionResult<AiAnalysisResultDto>> GetReport(long id, CancellationToken ct)
    {
        var report = await db.AiAnalysisReports.FindAsync([id], ct);
        if (report is null)
            return NotFound();

        return new AiAnalysisResultDto(report.Id, report.AnalysisText, report.GeneratedAtUtc, report.ModelUsed);
    }
}
