namespace TraceMonitor.Core.Models;

/// <summary>Generic Key/Value settings row, managed via the Settings tab — e.g. "Anthropic:ApiKey".
/// Deliberately minimal/generic so future settings don't each need their own table+migration.</summary>
public class AppSetting
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public string? Value { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
