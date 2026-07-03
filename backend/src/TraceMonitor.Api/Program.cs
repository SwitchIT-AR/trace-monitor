using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TraceMonitor.Core.Models;
using TraceMonitor.Core.Security;
using TraceMonitor.Infrastructure;
using TraceMonitor.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTraceMonitorInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "tm_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireClaim("isAdmin", "True"));
});

const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
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

    // First-run bootstrap: if no users exist yet, seed one admin from config so there's a way to
    // log in at all. Change the password via the Settings/Users UI immediately, then these can be
    // blanked out — this only ever runs once (skipped as soon as db.Users has any row).
    if (!db.Users.Any())
    {
        var username = app.Configuration["Auth:BootstrapAdminUsername"];
        var password = app.Configuration["Auth:BootstrapAdminPassword"];
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            var admin = new User { Username = username, PasswordHash = "", IsAdmin = true };
            admin.PasswordHash = UserPasswordHasher.Hash(admin, password);
            db.Users.Add(admin);
            db.SaveChanges();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(DevCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
