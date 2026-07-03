namespace TraceMonitor.Core.Models;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
