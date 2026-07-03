namespace TraceMonitor.Core.Services;

public interface ISettingsService
{
    Task<string?> GetAsync(string key, CancellationToken ct);
    Task SetAsync(string key, string value, CancellationToken ct);
}
