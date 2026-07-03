using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceMonitor.Api.Contracts;
using TraceMonitor.Core.Services;

namespace TraceMonitor.Api.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(Policy = "AdminOnly")]
public class SettingsController(ISettingsService settings) : ControllerBase
{
    [HttpGet("anthropic-key")]
    public async Task<ActionResult<MaskedKeyDto>> GetAnthropicKey(CancellationToken ct)
    {
        var key = await settings.GetAsync("Anthropic:ApiKey", ct);
        return new MaskedKeyDto(string.IsNullOrEmpty(key) ? null : $"...{key[^Math.Min(4, key.Length)..]}");
    }

    [HttpPut("anthropic-key")]
    public async Task<IActionResult> SetAnthropicKey(SetKeyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BadRequest("Value es requerido");

        await settings.SetAsync("Anthropic:ApiKey", request.Value, ct);
        return NoContent();
    }
}
