using TraceMonitor.Agent;
using TraceMonitor.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

var baseUrl = builder.Configuration["CentralApi:BaseUrl"]
    ?? throw new InvalidOperationException("Falta configurar CentralApi:BaseUrl (URL del backend central)");
var apiKey = builder.Configuration["CentralApi:ApiKey"]
    ?? throw new InvalidOperationException("Falta configurar CentralApi:ApiKey (clave del agente, generada con POST /api/agents)");

builder.Services.AddHttpClient("central-api", client =>
{
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("X-Agent-Key", apiKey);
});

builder.Services.AddSingleton<IMtrRunner, MtrRunner>();
builder.Services.AddHostedService<AgentWorker>();

var host = builder.Build();
host.Run();
