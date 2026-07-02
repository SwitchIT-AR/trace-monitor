using Microsoft.AspNetCore.Mvc;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Services;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController(IAiAnalysisService aiAnalysis) : ControllerBase
{
    [HttpPost("analyze")]
    public async Task<ActionResult<AiAnalysisResultDto>> Analyze(CancellationToken ct)
    {
        var result = await aiAnalysis.AnalyzeAsync(ct);
        return new AiAnalysisResultDto(result.AnalysisText, result.GeneratedAtUtc, result.ModelUsed);
    }
}
