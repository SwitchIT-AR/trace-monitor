using Microsoft.EntityFrameworkCore;
using TraceMonitor.Core.Models;

namespace TraceMonitor.Infrastructure.Data;

public class TraceMonitorDbContext(DbContextOptions<TraceMonitorDbContext> options) : DbContext(options)
{
    public DbSet<Target> Targets => Set<Target>();
    public DbSet<TraceRun> TraceRuns => Set<TraceRun>();
    public DbSet<TraceHop> TraceHops => Set<TraceHop>();
    public DbSet<PathChangeEvent> PathChangeEvents => Set<PathChangeEvent>();
    public DbSet<IpGeoCache> IpGeoCache => Set<IpGeoCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Target>(e =>
        {
            e.HasIndex(t => t.DestinationHost).IsUnique();
        });

        modelBuilder.Entity<TraceRun>(e =>
        {
            e.HasIndex(r => new { r.TargetId, r.StartedAtUtc });
            e.HasOne(r => r.Target).WithMany(t => t.Runs).HasForeignKey(r => r.TargetId);
        });

        modelBuilder.Entity<TraceHop>(e =>
        {
            e.HasIndex(h => h.TraceRunId);
            e.HasOne(h => h.TraceRun).WithMany(r => r.Hops).HasForeignKey(h => h.TraceRunId);
        });

        modelBuilder.Entity<PathChangeEvent>(e =>
        {
            e.HasIndex(ev => new { ev.TargetId, ev.DetectedAtUtc });
            e.HasOne(ev => ev.Target).WithMany(t => t.Events).HasForeignKey(ev => ev.TargetId);
        });

        modelBuilder.Entity<IpGeoCache>(e =>
        {
            e.HasKey(g => g.Ip);
        });

        modelBuilder.Entity<Target>().HasData(
            new Target { Id = 1, Name = "Griveo", Provider = "Telecentro", DestinationHost = "186.19.218.8", IsActive = true, CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Target { Id = 2, Name = "Vaclog", Provider = "IPLAN", DestinationHost = "190.210.245.120", IsActive = true, CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Target { Id = 3, Name = "Managio (DC)", Provider = "Telecentro", DestinationHost = "186.23.255.158", IsActive = true, CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Target { Id = 4, Name = "Vaclog (DC)", Provider = "Telecentro", DestinationHost = "186.23.255.156", IsActive = true, CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
