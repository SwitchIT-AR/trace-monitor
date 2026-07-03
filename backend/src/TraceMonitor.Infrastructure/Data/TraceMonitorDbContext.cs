using Microsoft.EntityFrameworkCore;
using TraceMonitor.Core.Models;

namespace TraceMonitor.Infrastructure.Data;

public class TraceMonitorDbContext(DbContextOptions<TraceMonitorDbContext> options) : DbContext(options)
{
    public DbSet<Target> Targets => Set<Target>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<TraceRun> TraceRuns => Set<TraceRun>();
    public DbSet<TraceHop> TraceHops => Set<TraceHop>();
    public DbSet<PathChangeEvent> PathChangeEvents => Set<PathChangeEvent>();
    public DbSet<IpGeoCache> IpGeoCache => Set<IpGeoCache>();
    public DbSet<RouteLabel> RouteLabels => Set<RouteLabel>();
    public DbSet<AiAnalysisReport> AiAnalysisReports => Set<AiAnalysisReport>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<UserTargetAgentAccess> UserTargetAgentAccess => Set<UserTargetAgentAccess>();

    /// <summary>Id of the seeded built-in agent that represents the office origin, fed in-process by TraceSchedulerWorker.</summary>
    public const int BuiltInAgentId = 1;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Target>(e =>
        {
            e.HasIndex(t => t.DestinationHost).IsUnique();
        });

        modelBuilder.Entity<Agent>(e =>
        {
            e.HasIndex(a => a.ApiKeyHash).IsUnique();
        });

        modelBuilder.Entity<TraceRun>(e =>
        {
            e.HasIndex(r => new { r.TargetId, r.AgentId, r.StartedAtUtc });
            e.HasIndex(r => new { r.TargetId, r.AgentId, r.RouteSignatureHash });
            e.HasOne(r => r.Target).WithMany(t => t.Runs).HasForeignKey(r => r.TargetId);
            e.HasOne(r => r.Agent).WithMany(a => a.Runs).HasForeignKey(r => r.AgentId);
        });

        modelBuilder.Entity<TraceHop>(e =>
        {
            e.HasIndex(h => h.TraceRunId);
            e.HasOne(h => h.TraceRun).WithMany(r => r.Hops).HasForeignKey(h => h.TraceRunId);
        });

        modelBuilder.Entity<PathChangeEvent>(e =>
        {
            e.HasIndex(ev => new { ev.TargetId, ev.AgentId, ev.DetectedAtUtc });
            e.HasOne(ev => ev.Target).WithMany(t => t.Events).HasForeignKey(ev => ev.TargetId);
            e.HasOne(ev => ev.Agent).WithMany(a => a.Events).HasForeignKey(ev => ev.AgentId);
        });

        modelBuilder.Entity<IpGeoCache>(e =>
        {
            e.HasKey(g => g.Ip);
        });

        modelBuilder.Entity<RouteLabel>(e =>
        {
            e.HasIndex(r => new { r.TargetId, r.AgentId, r.RouteSignatureHash }).IsUnique();
            e.HasOne(r => r.Target).WithMany().HasForeignKey(r => r.TargetId);
            e.HasOne(r => r.Agent).WithMany().HasForeignKey(r => r.AgentId);
        });

        modelBuilder.Entity<AiAnalysisReport>(e =>
        {
            e.HasIndex(r => r.GeneratedAtUtc);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.HasIndex(s => s.Key).IsUnique();
        });

        modelBuilder.Entity<UserTargetAgentAccess>(e =>
        {
            e.HasIndex(a => new { a.UserId, a.TargetId, a.AgentId }).IsUnique();
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);
            e.HasOne(a => a.Target).WithMany().HasForeignKey(a => a.TargetId);
            e.HasOne(a => a.Agent).WithMany().HasForeignKey(a => a.AgentId);
        });

        // Lat/Lon/Address are intentionally left null here — Program.cs fills them in at startup
        // from the (untracked) Office:* config, same source as OfficeController, so the real
        // coordinates never end up baked into a migration file.
        modelBuilder.Entity<Agent>().HasData(
            new Agent
            {
                Id = BuiltInAgentId, Name = "Oficina", Location = "Oficina", Provider = "Oficina",
                // Built-in agent is fed in-process (TraceSchedulerWorker), never authenticates over HTTP — this hash matches no real key.
                ApiKeyHash = "builtin-no-auth",
                IsActive = true, IsBuiltIn = true,
                CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            }
        );

        modelBuilder.Entity<Target>().HasData(
            new Target
            {
                Id = 1, Name = "Griveo", Provider = "Telecentro", DestinationHost = "186.19.218.8", IsActive = true,
                CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                VerifiedLat = -34.5816366, VerifiedLon = -58.4992073,
                VerifiedAddress = "Gral. José G. Artigas 4901, Villa Pueyrredón, CABA (Farmacia Nueva Social Fenix, suc. Griveo)",
            },
            new Target
            {
                Id = 2, Name = "Vaclog", Provider = "IPLAN", DestinationHost = "190.210.245.120", IsActive = true,
                CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                VerifiedLat = -34.6038067, VerifiedLon = -58.3842268,
                VerifiedAddress = "Av. Corrientes y 25 de Mayo, San Nicolás, CABA (aprox.)",
            },
            new Target
            {
                Id = 3, Name = "Managio (DC)", Provider = "Telecentro", DestinationHost = "186.23.255.158", IsActive = true,
                CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                VerifiedLat = -34.6712543, VerifiedLon = -58.5375181,
                VerifiedAddress = "Cnel. Pringles 3407, Lomas del Mirador, La Matanza (Datacenter Telecentro)",
            },
            new Target
            {
                Id = 4, Name = "Vaclog (DC)", Provider = "Telecentro", DestinationHost = "186.23.255.156", IsActive = true,
                CreatedAtUtc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
                VerifiedLat = -34.6712543, VerifiedLon = -58.5375181,
                VerifiedAddress = "Cnel. Pringles 3407, Lomas del Mirador, La Matanza (Datacenter Telecentro)",
            }
        );
    }
}
