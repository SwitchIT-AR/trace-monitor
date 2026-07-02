using Microsoft.EntityFrameworkCore;
using TraceMonitor.Infrastructure;
using TraceMonitor.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddTraceMonitorInfrastructure(builder.Configuration);

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraceMonitorDbContext>();
    db.Database.Migrate();

    // The built-in "Oficina" agent's coordinates live only in the (untracked) Office:* config —
    // same source as OfficeController — never in a migration, so real coordinates never end up
    // committed to source control.
    var builtInAgent = db.Agents.Find(TraceMonitorDbContext.BuiltInAgentId);
    if (builtInAgent is not null)
    {
        var office = app.Configuration.GetSection("Office");
        builtInAgent.Lat = office.GetValue<double?>("Lat");
        builtInAgent.Lon = office.GetValue<double?>("Lon");
        builtInAgent.Address = office.GetValue<string>("Address");
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(DevCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();
