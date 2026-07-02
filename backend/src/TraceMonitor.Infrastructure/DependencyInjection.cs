using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TraceMonitor.Core.Services;
using TraceMonitor.Infrastructure.Data;
using TraceMonitor.Infrastructure.Services;
using TraceMonitor.Infrastructure.Workers;

namespace TraceMonitor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTraceMonitorInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<TraceMonitorDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("TraceMonitor")));

        services.AddHttpClient("ip-api", client =>
        {
            client.BaseAddress = new Uri("http://ip-api.com/");
        });

        services.AddSingleton<IMtrRunner, MtrRunner>();
        services.AddScoped<IGeoIpService, GeoIpService>();
        services.AddSingleton<IPathChangeDetector, PathChangeDetector>();
        services.AddScoped<ITraceIngestionService, TraceIngestionService>();
        services.AddScoped<IAgentAuthenticator, AgentAuthenticator>();
        services.AddScoped<IAiAnalysisService, AiAnalysisService>();
        services.AddHostedService<TraceSchedulerWorker>();

        return services;
    }
}
