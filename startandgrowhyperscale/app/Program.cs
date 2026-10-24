using CaldovaNetworkScheduling.Components;
using CaldovaNetworkScheduling.Services;
using Azure.Core;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

var demoMode = builder.Configuration["Demo:Mode"] ?? "Measured";
if (!string.Equals(demoMode, "Measured", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException($"Demo:Mode '{demoMode}' is not supported. Use 'Measured'; scripted playback is no longer the live demo.");
}

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<TokenCredential>(new DefaultAzureCredential());
builder.Services.AddHttpClient<ServerlessMonitor>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<ServerlessSqlWorkload>();
builder.Services.AddScoped<ServerlessSqlTelemetry>();
builder.Services.AddScoped<ServerlessResume>();
builder.Services.AddOptions<ServerlessScriptOptions>();
builder.Services.AddScoped<IScaleWorkload, ScriptedScaleWorkload>();
builder.Services.AddSingleton<IAppointmentAvailabilityService, SqlAppointmentAvailabilityService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Json(new { status = "ok", mode = "measured", serverless = "sql-dmv", appointments = "sql" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
