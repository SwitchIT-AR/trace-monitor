using System.Text;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Services;

/// <summary>
/// Builds a small, pre-aggregated context bundle (loss summary, flagged hop-loss, route-flap
/// summary, recent path-change events — never raw per-run rows) and asks Claude to review it as
/// a senior network/security engineer. Keeping the bundle aggregated is what keeps a fleet-wide
/// analysis in the low thousands of input tokens instead of the ~1-2M tokens/day a raw dump would cost.
/// </summary>
public class AiAnalysisService(TraceMonitorDbContext db, IConfiguration config, ISettingsService settings) : IAiAnalysisService
{
    private const double HopLossFlagThresholdPct = 5.0;

    private const string SystemPrompt =
        "Sos un ingeniero senior de redes y ciberseguridad revisando el comportamiento de rutas " +
        "de un monitor de red (traceroutes periodicos hacia varios destinos, desde uno o mas agentes). " +
        "Se te da un resumen ya agregado (no datos crudos por corrida). Analiza: (1) perdida de paquetes " +
        "anormal o asimetrica entre agentes hacia el mismo destino, (2) flapping de rutas frecuente hacia " +
        "ASNs no vistos antes (mas preocupante que alternar siempre entre las 2 mismas rutas conocidas), " +
        "(3) perdida concentrada en un salto puntual vs distribuida en todo el camino, (4) cualquier otra " +
        "anomalia de seguridad o de red que veas en los datos. Respondé en español, en prosa clara para " +
        "una persona (no JSON), con un par de parrafos y, si corresponde, una lista de hallazgos concretos.";

    public async Task<AiAnalysisResult> AnalyzeAsync(CancellationToken ct)
    {
        var apiKey = await settings.GetAsync("Anthropic:ApiKey", ct);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // One-time fallback: migrate whatever was in the old .env/appsettings config into the
            // DB, so upgrading doesn't silently lose a key already configured before Settings existed.
            apiKey = config["Anthropic:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKey))
                await settings.SetAsync("Anthropic:ApiKey", apiKey, ct);
        }

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic:ApiKey no esta configurada (ver pestaña Settings)");

        var model = await settings.GetAsync("Anthropic:Model", ct) ?? config["Anthropic:Model"] ?? "claude-opus-4-8";

        var targetNames = await db.Targets.ToDictionaryAsync(t => t.Id, t => t.Name, ct);
        var agentNames = await db.Agents.ToDictionaryAsync(a => a.Id, a => a.Name, ct);

        var lossSummary24h = await BuildLossSummaryAsync(24, targetNames, agentNames, ct);
        var lossSummary7d = await BuildLossSummaryAsync(24 * 7, targetNames, agentNames, ct);
        var hopLossSection = await BuildFlaggedHopLossAsync(targetNames, agentNames, ct);
        var routeFlapSection = await BuildRouteFlapSummaryAsync(targetNames, agentNames, ct);
        var eventsSection = await BuildRecentEventsAsync(targetNames, agentNames, ct);

        var userPrompt = new StringBuilder()
            .AppendLine("## Resumen de perdida de paquetes (ultimas 24h, por target+agente)")
            .AppendLine(lossSummary24h.Count == 0 ? "(sin datos)" : string.Join('\n', lossSummary24h))
            .AppendLine()
            .AppendLine("## Resumen de perdida de paquetes (ultimos 7 dias, por target+agente)")
            .AppendLine(lossSummary7d.Count == 0 ? "(sin datos)" : string.Join('\n', lossSummary7d))
            .AppendLine()
            .AppendLine("## Perdida por salto (solo pares con perdida 24h > 5%)")
            .AppendLine(hopLossSection.Count == 0 ? "(ningun par supera el umbral)" : string.Join('\n', hopLossSection))
            .AppendLine()
            .AppendLine("## Estabilidad de rutas (ultimos 7 dias, por target+agente)")
            .AppendLine(routeFlapSection.Count == 0 ? "(sin datos)" : string.Join('\n', routeFlapSection))
            .AppendLine()
            .AppendLine("## Cambios de ruta recientes (ultimas 48h, hasta 30)")
            .AppendLine(eventsSection.Count == 0 ? "(sin cambios recientes)" : string.Join('\n', eventsSection))
            .ToString();

        var client = new AnthropicClient { ApiKey = apiKey };
        var response = await client.Messages.Create(
            new MessageCreateParams
            {
                Model = model,
                MaxTokens = 4096,
                System = SystemPrompt,
                Messages = [new MessageParam { Role = Role.User, Content = userPrompt }],
            },
            cancellationToken: ct);

        var text = string.Join(
            "\n\n",
            response.Content
                .Select(c => c.TryPickText(out var t) ? t.Text : null)
                .Where(t => !string.IsNullOrEmpty(t)));

        var report = new AiAnalysisReport { AnalysisText = text, GeneratedAtUtc = DateTime.UtcNow, ModelUsed = model };
        db.AiAnalysisReports.Add(report);
        await db.SaveChangesAsync(ct);

        return new AiAnalysisResult(report.Id, report.AnalysisText, report.GeneratedAtUtc, report.ModelUsed);
    }

    private async Task<List<string>> BuildLossSummaryAsync(
        int hours, Dictionary<int, string> targetNames, Dictionary<int, string> agentNames, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var grouped = await db.TraceRuns
            .Where(r => r.StartedAtUtc >= since)
            .GroupBy(r => new { r.TargetId, r.AgentId })
            .Select(g => new
            {
                g.Key.TargetId,
                g.Key.AgentId,
                AvgLossPct = g.Average(r => r.OverallLossPct),
                RunCount = g.Count(),
            })
            .ToListAsync(ct);

        return grouped
            .Where(g => targetNames.ContainsKey(g.TargetId) && agentNames.ContainsKey(g.AgentId))
            .OrderByDescending(g => g.AvgLossPct)
            .Select(g => $"- {targetNames[g.TargetId]} / {agentNames[g.AgentId]}: {g.AvgLossPct:F1}% perdida promedio ({g.RunCount} corridas)")
            .ToList();
    }

    private async Task<List<string>> BuildFlaggedHopLossAsync(
        Dictionary<int, string> targetNames, Dictionary<int, string> agentNames, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var flaggedPairs = await db.TraceRuns
            .Where(r => r.StartedAtUtc >= since)
            .GroupBy(r => new { r.TargetId, r.AgentId })
            .Select(g => new { g.Key.TargetId, g.Key.AgentId, AvgLossPct = g.Average(r => r.OverallLossPct) })
            .Where(g => g.AvgLossPct > HopLossFlagThresholdPct)
            .ToListAsync(ct);

        var result = new List<string>();
        foreach (var pair in flaggedPairs)
        {
            var runIds = db.TraceRuns
                .Where(r => r.TargetId == pair.TargetId && r.AgentId == pair.AgentId && r.StartedAtUtc >= since)
                .Select(r => r.Id);

            var hopGroups = await db.TraceHops
                .Where(h => runIds.Contains(h.TraceRunId))
                .GroupBy(h => new { h.HopIndex, h.Ip })
                .Select(g => new { g.Key.HopIndex, g.Key.Ip, AvgLossPct = g.Average(h => h.LossPct) })
                .Where(g => g.AvgLossPct > HopLossFlagThresholdPct)
                .OrderBy(g => g.HopIndex)
                .ToListAsync(ct);

            if (hopGroups.Count == 0)
                continue;

            var label = $"{targetNames.GetValueOrDefault(pair.TargetId, "?")} / {agentNames.GetValueOrDefault(pair.AgentId, "?")}";
            foreach (var hop in hopGroups)
                result.Add($"- {label}: salto #{hop.HopIndex} ({hop.Ip ?? "sin respuesta"}) con {hop.AvgLossPct:F1}% perdida promedio");
        }

        return result;
    }

    private async Task<List<string>> BuildRouteFlapSummaryAsync(
        Dictionary<int, string> targetNames, Dictionary<int, string> agentNames, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-24 * 7);

        var runs = await db.TraceRuns
            .Where(r => r.StartedAtUtc >= since)
            .OrderBy(r => r.StartedAtUtc)
            .Select(r => new { r.TargetId, r.AgentId, r.StartedAtUtc, r.RouteSignatureHash })
            .ToListAsync(ct);

        var labels = await db.RouteLabels
            .ToDictionaryAsync(l => (l.TargetId, l.AgentId, l.RouteSignatureHash), l => l.Label, ct);

        var result = new List<string>();
        foreach (var pairGroup in runs.GroupBy(r => (r.TargetId, r.AgentId)))
        {
            var ordered = pairGroup.OrderBy(r => r.StartedAtUtc).ToList();
            var segments = new List<string?>();
            foreach (var run in ordered)
            {
                if (segments.Count == 0 || segments[^1] != run.RouteSignatureHash)
                    segments.Add(run.RouteSignatureHash);
            }

            var distinctRoutes = segments.Distinct().Count();
            var flapCount = segments.Count - 1;
            var lastHash = ordered[^1].RouteSignatureHash;
            var lastLabel = lastHash is not null && labels.TryGetValue((pairGroup.Key.TargetId, pairGroup.Key.AgentId, lastHash), out var l)
                ? l
                : "ruta desconocida";

            var label = $"{targetNames.GetValueOrDefault(pairGroup.Key.TargetId, "?")} / {agentNames.GetValueOrDefault(pairGroup.Key.AgentId, "?")}";
            result.Add($"- {label}: {distinctRoutes} ruta(s) distinta(s), {flapCount} cambio(s) en 7 dias, ruta actual: {lastLabel}");
        }

        return result;
    }

    private async Task<List<string>> BuildRecentEventsAsync(
        Dictionary<int, string> targetNames, Dictionary<int, string> agentNames, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddHours(-48);

        var events = await db.PathChangeEvents
            .Where(e => e.DetectedAtUtc >= since)
            .OrderByDescending(e => e.DetectedAtUtc)
            .Take(30)
            .Select(e => new { e.TargetId, e.AgentId, e.DetectedAtUtc })
            .ToListAsync(ct);

        return events
            .Select(e => $"- {targetNames.GetValueOrDefault(e.TargetId, "?")} / {agentNames.GetValueOrDefault(e.AgentId, "?")}: cambio de ruta detectado {e.DetectedAtUtc:yyyy-MM-dd HH:mm} UTC")
            .ToList();
    }
}
