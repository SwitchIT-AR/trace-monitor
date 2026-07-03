using Microsoft.EntityFrameworkCore;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;

namespace TraceMonitor.Infrastructure.Services;

public class SettingsService(TraceMonitorDbContext db) : ISettingsService
{
    public async Task<string?> GetAsync(string key, CancellationToken ct) =>
        (await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct))?.Value;

    public async Task SetAsync(string key, string value, CancellationToken ct)
    {
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedAtUtc = DateTime.UtcNow });
        }
        else
        {
            row.Value = value;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
