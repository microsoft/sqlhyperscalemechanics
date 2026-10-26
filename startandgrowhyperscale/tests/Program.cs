using CaldovaNetworkScheduling.Components.Layout;
using CaldovaNetworkScheduling.Components.Pages;
using CaldovaNetworkScheduling.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Azure.Core;
using Azure.Identity;
using System.Text.Json;
using System.Net;

var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException($"FAIL: {description}");
    checks++;
}

var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ServerlessMonitor:ResourceId"] = "/subscriptions/0efc44aa-c965-420f-aac4-fff305dbcc97/resourceGroups/caldova-network-rg/providers/Microsoft.Sql/servers/sql-caldova-network-0efc44/databases/caldova-scheduling",
    ["ServerlessMonitor:EnableWorkload"] = "false"
}).AddEnvironmentVariables().Build();
if (args.Contains("--serverless-live"))
{
    var ramped = args.Contains("--serverless-ramp");
    var workers = ramped ? 8 : 4;
    var workerIndex = Array.IndexOf(args, "--serverless-workers");
    if (workerIndex >= 0 && (workerIndex + 1 >= args.Length
        || !int.TryParse(args[workerIndex + 1], out workers) || workers is < 1 or > 16))
        throw new ArgumentException("--serverless-workers must be an integer from 1 to 16.");
    int Seconds(string name, int fallback)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0) return fallback;
        if (!ramped || index + 1 >= args.Length || !int.TryParse(args[index + 1], out var value))
            throw new ArgumentException($"{name} requires --serverless-ramp and an integer number of seconds.");
        return value;
    }
    var liveRamp = ramped ? new WorkloadRamp(workers,
        TimeSpan.FromSeconds(Seconds("--serverless-ramp-step-seconds", 30)),
        TimeSpan.FromSeconds(Seconds("--serverless-ramp-hold-seconds", 60)),
        TimeSpan.FromSeconds(Seconds("--serverless-ramp-down-step-seconds", Seconds("--serverless-ramp-step-seconds", 30)))) : null;
    await ServerlessLiveCheck.RunAsync(configuration, workers, liveRamp);
    return;
}
var observeIndex = Array.IndexOf(args, "--serverless-observe-from");
var sqlObserveIndex = Array.IndexOf(args, "--serverless-dmv-observe-from");
if (sqlObserveIndex >= 0)
{
    if (sqlObserveIndex + 1 >= args.Length
        || !DateTimeOffset.TryParse(args[sqlObserveIndex + 1], out var sqlFrom))
        throw new ArgumentException("--serverless-dmv-observe-from requires a UTC timestamp.");
    await ServerlessLiveCheck.ObserveSqlAsync(configuration, sqlFrom);
    return;
}
if (observeIndex >= 0)
{
    await ServerlessLiveCheck.CaptureAsync(configuration, DateTimeOffset.Parse(args[observeIndex + 1]));
    return;
}
var sql = new SqlAppointmentAvailabilityService(configuration);
Check(new ServerlessCpuSample(DateTimeOffset.UtcNow, 50, 8).UsedVCores == 4,
    "DMV CPU uses the sample's own vCore limit");
Check(new ServerlessCpuSample(DateTimeOffset.UtcNow, 0, 8).UsedVCores == 0,
    "DMV CPU preserves a real zero");
try
{
    _ = new ServerlessCpuSample(DateTimeOffset.UtcNow, 50, 0).UsedVCores;
    throw new InvalidOperationException("Invalid DMV limit was accepted");
}
catch (InvalidOperationException ex) when (ex.Message.StartsWith("SQL returned")) { checks++; }
foreach (var invalid in new[] { new ServerlessCpuSample(DateTimeOffset.UtcNow, 101, 8),
    new ServerlessCpuSample(DateTimeOffset.UtcNow, double.NaN, 8),
    new ServerlessCpuSample(DateTimeOffset.UtcNow, 50, double.PositiveInfinity) })
{
    try
    {
        _ = invalid.UsedVCores;
        throw new Exception("Invalid SQL CPU sample was accepted");
    }
    catch (InvalidOperationException) { checks++; }
}
var sessionTracker = new SessionCpuTracker();
var sessionAt = new DateTime(2026, 10, 22, 18, 0, 0, DateTimeKind.Utc);
var sessionLogin = sessionAt.AddMinutes(-1);
Check(sessionTracker.Observe(sessionAt, [new(100, sessionLogin, 1000)]) is null,
    "Session CPU needs a baseline");
Check(sessionTracker.Observe(sessionAt.AddSeconds(15), [new(100, sessionLogin, 61000)]) == 4,
    "Session CPU delta divides CPU milliseconds by elapsed milliseconds");
Check(sessionTracker.Observe(sessionAt.AddSeconds(30), []) is null,
    "Disappeared session counters are not zero CPU");
Check(sessionTracker.Observe(sessionAt.AddSeconds(45), [new(100, sessionAt.AddSeconds(35), 30000)]) == 2,
    "Reused session IDs use login time and count only the new session's CPU");
Check(sql.Mode == "SQL", "Appointments must not silently fall back to scripted data");
Check(sql.ViewDefinition.Contains("JOIN [northbrook-scheduling].dbo.AppointmentSlot"), "View joins Northbrook");
Check(sql.ViewDefinition.Contains("JOIN [ridgeview-scheduling].dbo.AppointmentSlot"), "View joins Ridgeview");
Check(sql.ViewDefinition.Split("FROM network.ProviderClinic").Length == 4, "All providers join the central directory");
Check(sql.ViewDefinition.Split("p.IsNetworkMember = 1 AND s.IsBooked = 0").Length == 4, "All branches filter membership and booked slots");
foreach (var key in new[] { "caldova", "northbrook", "ridgeview" })
{
    var query = SqlAppointmentAvailabilityService.GetBookingQuery(key);
    var database = SqlAppointmentAvailabilityService.GetDatabase(key);
    Check(query.Split("UPDATE ").Length == 2 && query.Split("INSERT ").Length == 2, $"{key} booking has one UPDATE and one INSERT");
    Check(query.Contains($"FROM {SqlAppointmentAvailabilityService.GetSourceObject(key)} AS s")
        && query.Contains($"INSERT [{database}].dbo.Appointment"), $"{key} writes only the allowlisted provider database");
    Check(new[] { "caldova-scheduling", "northbrook-scheduling", "ridgeview-scheduling" }.Where(d => d != database).All(d => !query.Contains($"[{d}]")),
        $"{key} transaction writes to exactly one database");
    Check(query.Contains("s.IsBooked = 0") && query.Contains("s.SlotId = @SlotId") && query.Contains("IF @@ROWCOUNT = 1"), $"{key} conditional slot claim");
    Check(query.Contains("BEGIN TRANSACTION") && query.Contains("COMMIT TRANSACTION") && query.Contains("SET XACT_ABORT ON")
        && !query.Contains("SAVE TRAN"), $"{key} one atomic transaction without savepoints");
    Check(query.Contains("p.ProviderKey = @ProviderKey") && query.Contains("p.IsNetworkMember = 1"), $"{key} membership guard");
}
try
{
    SqlAppointmentAvailabilityService.GetBookingQuery("ridgeview]; DROP TABLE dbo.AppointmentSlot;--");
    throw new InvalidOperationException("Untrusted provider was accepted");
}
catch (ArgumentException) { checks++; }
try
{
    await sql.BookAsync("caldova", 0);
    throw new InvalidOperationException("Invalid slot was accepted");
}
catch (ArgumentOutOfRangeException) { checks++; }
try
{
    await sql.FindNextAsync("unrecognized", true);
    throw new InvalidOperationException("Invalid specialty was accepted");
}
catch (ArgumentException) { checks++; }

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(configuration);
services.AddSingleton<TokenCredential>(new DefaultAzureCredential());
services.AddHttpClient<ServerlessMonitor>();
services.AddScoped<ServerlessSqlWorkload>();
services.AddScoped<ServerlessSqlTelemetry>();
services.AddScoped<ServerlessResume>();
services.AddOptions<ServerlessScriptOptions>();
services.AddScoped<IScaleWorkload, ScriptedScaleWorkload>();
services.AddSingleton<IAppointmentAvailabilityService>(sql);
services.AddSingleton<NavigationManager>(new CheckNavigation());
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
async Task<string> RenderAsync<T>() where T : IComponent =>
    await renderer.Dispatcher.InvokeAsync(async () =>
        (await renderer.RenderComponentAsync<T>()).ToHtmlString());

var layout = await RenderAsync<MainLayout>();
foreach (var route in new[] { "/network", "/verify", "/appointments" })
    Check(layout.Contains($"href=\"{route}\""), $"Navigation includes {route}");
Check(layout.Contains("Serverless: SQL DMV"), "Layout identifies actual serverless metric source");
var acquisition = await RenderAsync<Overview>();
var patientAccess = await RenderAsync<Scale>();
Check(!patientAccess.Contains("id=\"billing\"") && !patientAccess.Contains("Open Caldova database in Azure portal")
    && !patientAccess.Contains("End of Part 1") && !patientAccess.Contains("Presenter setup"), "Measured page has no presenter or portal instructions");
Check(patientAccess.Contains("Behind the chart") && patientAccess.Contains("HASHBYTES"), "Measured page shows the SQL workload behind the chart");
Check(patientAccess.Contains("avg_cpu_percent × cpu_limit / 100") && patientAccess.Contains("sys.dm_db_resource_stats"), "SQL CPU calculation and source are disclosed");
Check(patientAccess.Contains("▶ Run") && patientAccess.Contains("Missing samples are not filled with zeros")
    && patientAccess.Contains("measured vCores") && !patientAccess.Contains("scripted vCores"), "Measured mode uses the shared scene and waits for actual SQL metrics");
Check(patientAccess.Contains("prevent auto-pause") && patientAccess.Contains("VIEW DATABASE STATE"),
    "Behind the chart discloses SQL observation keeps the database awake and requires SQL permission");
Check(patientAccess.Contains("15-second") && patientAccess.Contains("55 minutes")
    && patientAccess.Contains("chart does not use Azure Monitor"), "SQL chart discloses interval, retention window, and measurement source");
Check(!patientAccess.Contains("Next · Step 2"), "Serverless chapter ends independently");
Check(patientAccess.Contains("staff online") && patientAccess.Contains("Each staff member is one SQL worker")
    && patientAccess.Contains("illustrative entries") && !patientAccess.Contains("Press Play"), "Measured staff map to real workers; patient entries are disclosed as illustrative");
Check(!patientAccess.Contains("<strong>Measured.</strong>"), "No Measured details box at the top");
Check(patientAccess.Contains("Load generation is disabled"), "SQL load is opt-in");
var illustration = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<Scale>(
    ParameterView.FromDictionary(new Dictionary<string, object?> { ["ForceScripted"] = true }))).ToHtmlString());
Check(illustration.Contains("SCRIPTED ILLUSTRATION") && illustration.Contains("Not Azure telemetry"), "Fallback prominently distinguishes scripted illustration");
Check(illustration.Contains("runs no SQL workload") && illustration.Contains("generates no billing evidence"), "Fallback claims no real load or billing");
Check(illustration.Contains("app_cpu_percent") && illustration.Contains("app_cpu_billed")
    && illustration.Contains("Open Caldova database in Azure portal"), "Fallback still requires real portal metrics");
Check(!patientAccess.Contains("href=\"/serverless-illustration\"") && !illustration.Contains(">Measured<")
    && illustration.Contains("scripted vCores") && !illustration.Contains("measured vCores"), "No Scripted/Measured buttons; the illustration (by URL only) still labels its mode");
var scriptOptions = new ServerlessScriptOptions();
Check(scriptOptions.MinVCores + scriptOptions.PeakUsers * scriptOptions.VCoresPerUser == 4
    && scriptOptions.MaxVCores == 8, "Illustration peaks at 4 used vCores without changing the 8-vCore maximum");
Check(patientAccess.Contains("Sign in (resume)") && !illustration.Contains("Sign in (resume)"), "Measured mode offers an explicit first sign-in to resume");
Check(ServerlessResume.Transient.Contains(40613) && ServerlessResume.Transient.Contains(40197) && !ServerlessResume.Transient.Contains(18456)
    && ServerlessResume.Backoff(1) == TimeSpan.FromSeconds(1) && ServerlessResume.Backoff(10) == TimeSpan.FromSeconds(10),
    "Resume retries only transient errors (not login failures) with capped backoff");
Check(illustration.Contains("without hovering") && illustration.Contains("no compute charge during confirmed auto-pause"),
    "Portal beat is a visual billed-compute story, not a hover-number comparison");
Check(acquisition.Contains("northbrook-scheduling") && acquisition.Contains("ridgeview-scheduling"), "Acquisition shows the three provider databases");
Check(!acquisition.Contains("Add these databases to a pool") && !acquisition.Contains("Copilot") && !acquisition.Contains("Next ·"),
    "Acquisition has no presenter instructions or step buttons");
Check(!acquisition.Contains("Integrate into elastic pool") && !acquisition.Contains("Load history"), "No simulated Azure mutation buttons");
var verify = await RenderAsync<Verify>();
Check(verify.Contains("Check network") && verify.Contains("caldova-network-pool")
    && verify.Contains("[ridgeview-scheduling].dbo.AppointmentSlot"), "Network page shows pool membership and three-part-name reads");
Check(!verify.Contains("portal.azure.com") && !verify.Contains("Open pool in Azure portal"), "Network page has no portal instructions");
var appointments = await RenderAsync<Appointments>();
Check(appointments.Contains("Find openings") && appointments.Contains("Search the entire network"), "Explicit local/network search");
Check(appointments.Contains("Step 4") && appointments.Contains("UTC"), "Search shows scenario step and timezone");
Check(!appointments.Contains("No openings"), "Initial render does not pretend SQL ran");
Check(!appointments.Contains("Search Caldova alone first") && !appointments.Contains("Back ·"), "Search page has no presenter hints");

var start = new DateTimeOffset(2026, 10, 22, 15, 0, 0, TimeSpan.Zero);
JsonDocument Metric(string name, string unit, string aggregation, params double?[] values) =>
    JsonDocument.Parse(JsonSerializer.Serialize(new
    {
        interval = "PT1M",
        value = new[]
        {
            new
            {
                name = new { value = name }, unit,
                timeseries = new[]
                {
                    new
                    {
                        data = values.Select((value, index) => new Dictionary<string, object?>
                        {
                            ["timeStamp"] = start.AddMinutes(index), [aggregation] = value
                        })
                    }
                }
            }
        }
    }));
using var cpuMetric = Metric("app_cpu_percent", "Percent", "average", 25, null, 0, 50);
using var billedMetric = Metric("app_cpu_billed", "Count", "total", 180, 60, 0, null);
using var memoryMetric = Metric("app_memory_percent", "Percent", "average", 8.5, null, 0, 9);
var withMemory = ServerlessMonitor.Combine(cpuMetric.RootElement, billedMetric.RootElement,
    start, start.AddMinutes(4), memoryMetric.RootElement);
Check(withMemory[0].MemoryPercent == 8.5 && withMemory[1].MemoryPercent is null && withMemory[2].MemoryPercent == 0,
    "Memory percentages retain real values, missing buckets, and actual zeros independently");
Check(ServerlessSqlWorkload.Query.Contains("varbinary(32)") && ServerlessSqlWorkload.Query.Contains("50000")
    && ServerlessSqlWorkload.Query.Contains("HASHBYTES('SHA2_256'")
    && !ServerlessSqlWorkload.Query.Contains(" FROM ") && !ServerlessSqlWorkload.Query.Contains(" JOIN ")
    && !ServerlessSqlWorkload.Query.Contains("INSERT") && !ServerlessSqlWorkload.Query.Contains("DBCC"),
    "CPU query uses bounded scalar computation without table reads, joins, data writes, or cache flushes");
try
{
    using var invalidMemory = Metric("app_memory_percent", "Percent", "average", 101);
    ServerlessMonitor.Combine(cpuMetric.RootElement, billedMetric.RootElement, start, start.AddMinutes(1), invalidMemory.RootElement);
    throw new Exception("Invalid memory percentage accepted");
}
catch (InvalidOperationException) { checks++; }
var measured = ServerlessMonitor.Combine(cpuMetric.RootElement, billedMetric.RootElement, start, start.AddMinutes(4));
Check(measured.Count == 4, "Every completed minute is represented");
Check(measured[0].UsedVCores(8) == 2 && measured[0].BilledVCores == 3, "25% of 8 is 2 used; 180 vCore-seconds is 3 billed");
Check(measured[1].UsedVCores(8) is null && measured[1].BilledVCores == 1, "Missing CPU stays missing independently of billed samples");
Check(measured[2].UsedVCores(8) == 0 && measured[2].BilledVCores == 0, "Actual zero samples remain zero");
Check(measured[3].BilledVCores is null, "Missing billed sample does not become zero");
var filtered = ServerlessMonitor.Combine(cpuMetric.RootElement, billedMetric.RootElement, start.AddMinutes(1), start.AddMinutes(3));
Check(filtered.Count == 2 && filtered[0].At == start.AddMinutes(1), "Only requested complete buckets are included");
Check(ServerlessMonitor.Minute(start.AddSeconds(59)) == start, "Bucket boundary excludes unfinished minute");
try
{
    using var invalidCpu = Metric("app_cpu_percent", "Percent", "average", 101);
    ServerlessMonitor.Combine(invalidCpu.RootElement, billedMetric.RootElement, start, start.AddMinutes(1));
    throw new Exception("Invalid CPU percentage accepted");
}
catch (InvalidOperationException) { checks++; }
try
{
    using var invalidUnit = Metric("app_cpu_billed", "Percent", "total", 180);
    ServerlessMonitor.Combine(cpuMetric.RootElement, invalidUnit.RootElement, start, start.AddMinutes(1));
    throw new Exception("Invalid billed unit accepted");
}
catch (InvalidOperationException) { checks++; }
using var metricHandler = new MonitorHandler(cpuMetric.RootElement.GetRawText(), billedMetric.RootElement.GetRawText(), memoryMetric.RootElement.GetRawText());
using var metricHttp = new HttpClient(metricHandler);
var monitor = new ServerlessMonitor(metricHttp, new TestCredential(), configuration);
var baseline = await monitor.ReadConfigurationAsync(CancellationToken.None);
Check(baseline.MinVCores == 1 && baseline.MaxVCores == 8, "Maximum/minimum come from ARM, not configured display constants");
var wrongSqlTarget = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Scheduling"] = "Server=tcp:other.database.windows.net,1433;Initial Catalog=caldova-scheduling"
}).Build();
try
{
    await new ServerlessSqlTelemetry(wrongSqlTarget).ReadAsync(baseline, start, CancellationToken.None);
    throw new Exception("Mismatched SQL telemetry target was accepted");
}
catch (InvalidOperationException ex) when (ex.Message.StartsWith("SQL target")) { checks++; }
var throughApi = await monitor.ReadMetricsAsync(baseline, start, start.AddMinutes(4).AddSeconds(30), CancellationToken.None);
Check(throughApi.Count == 4, "HTTP metric flow omits the currently unfinished minute");
Check(metricHandler.Requests.Count == 5, "Read baseline/configuration then separate CPU, billed, and memory requests");
Check(throughApi[0].MemoryPercent == 8.5
    && metricHandler.Requests.Any(r => r.Contains("metricnames=app_memory_percent&aggregation=Average")), "HTTP flow captures actual app memory percentage");
Check(metricHandler.Requests.Any(r => r.Contains("metricnames=app_cpu_percent&aggregation=Average"))
    && metricHandler.Requests.Any(r => r.Contains("metricnames=app_cpu_billed&aggregation=Total"))
    && metricHandler.Requests.Where(r => r.Contains("/metrics?")).All(r => r.Contains("interval=PT1M")), "API uses the exact metric/aggregation/granularity contract");
Check(metricHandler.Requests.Where(r => r.Contains("/metrics?")).All(r => !r.Contains("%2B") && !r.Contains("+00")),
    "Metric request timestamps use UTC Z, not offsets Azure decodes as spaces");
metricHandler.Capacity = 4;
try
{
    await monitor.ReadMetricsAsync(baseline, start, start.AddMinutes(4), CancellationToken.None);
    throw new Exception("Configuration change accepted");
}
catch (InvalidOperationException) { checks++; }
metricHandler.Deny = true;
try
{
    await monitor.ReadConfigurationAsync(CancellationToken.None);
    throw new Exception("Monitoring permission error accepted");
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden) { checks++; }
await using (var disabledLoad = new ServerlessSqlWorkload(configuration, provider.GetRequiredService<ILogger<ServerlessSqlWorkload>>()))
{
    try
    {
        disabledLoad.Start(new("/subscriptions/test", "Online", 1, 8), 4, 3);
        throw new Exception("Disabled workload accepted");
    }
    catch (InvalidOperationException) { checks++; }
    try
    {
        disabledLoad.StartRamp(new("/subscriptions/test", "Online", 1, 8), new(8, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)));
        throw new Exception("Disabled ramp accepted");
    }
    catch (InvalidOperationException) { checks++; }
}
var ramp = new WorkloadRamp(8, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
ramp.Validate();
Check(ramp.Total == TimeSpan.FromMinutes(8), "8-worker ramp with 30s steps and 60s hold lasts 8 minutes");
Check(Enumerable.Range(0, 16).Select(s => ramp.ActiveAt(TimeSpan.FromSeconds(s * 30 + 1)))
        .SequenceEqual([1, 2, 3, 4, 5, 6, 7, 8, 8, 7, 6, 5, 4, 3, 2, 1]) && ramp.ActiveAt(ramp.Total) == 0,
    "Ramp adds one worker per step, holds at peak, then removes one per step to zero");
Check(Enumerable.Range(0, 8).All(i => ramp.StopAt(i) > ramp.StartAt(i)) && ramp.StopAt(7) < ramp.StopAt(0),
    "Every ramp worker runs, and the last worker started stops first");
var demoRamp = new WorkloadRamp(8, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(45));
demoRamp.Validate();
Check(demoRamp.PeakAt == TimeSpan.FromSeconds(105) && demoRamp.Total == TimeSpan.FromMinutes(8)
    && demoRamp.ActiveAt(TimeSpan.FromSeconds(106)) == 8 && demoRamp.ActiveAt(TimeSpan.FromSeconds(166)) == 7
    && demoRamp.ActiveAt(TimeSpan.FromSeconds(436)) == 1 && demoRamp.ActiveAt(demoRamp.Total) == 0,
    "Demo ramp reaches 8 workers in 105 s, holds 60 s, then steps down every 45 s to zero at 8 minutes");
foreach (var invalid in new WorkloadRamp[] { new(0, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)),
    new(17, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)), new(8, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60)),
    new(8, TimeSpan.FromSeconds(30), TimeSpan.Zero), new(16, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)) })
{
    try
    {
        invalid.Validate();
        throw new Exception($"Invalid ramp accepted: {invalid}");
    }
    catch (ArgumentOutOfRangeException) { checks++; }
}

var playback = new CaldovaNetworkScheduling.Services.Playback.PlaybackAppointmentAvailabilityService();
var caldovaOnly = await playback.FindNextAsync("Cardiology", networkWide: false);
var network = await playback.FindNextAsync("Cardiology", networkWide: true);
Check(caldovaOnly.QueryText == SqlAppointmentAvailabilityService.LocalQuery && network.QueryText == SqlAppointmentAvailabilityService.NetworkQuery
    && playback.ViewDefinition == SqlAppointmentAvailabilityService.ViewSql, "Playback shows the live query and view SQL");
Check(caldovaOnly.Slots.All(s => s.ProviderKey == "caldova") && network.Slots[0].ProviderKey == "ridgeview"
    && network.Slots[0].SourceObject == "[ridgeview-scheduling].dbo.AppointmentSlot"
    && (network.Slots[0].SlotStart.Date - DateTime.UtcNow.Date).Days == 2
    && (caldovaOnly.Slots[0].SlotStart.Date - DateTime.UtcNow.Date).Days == 19,
    "Playback seeds match the schema script: Caldova Cardiology +19 days, Ridgeview +2 days");
var firstBooking = await playback.BookAsync("ridgeview", network.Slots[0].SlotId);
var secondBooking = await playback.BookAsync("ridgeview", network.Slots[0].SlotId);
Check(firstBooking.Booked && !secondBooking.Booked && firstBooking.QueryText == SqlAppointmentAvailabilityService.GetBookingQuery("ridgeview"),
    "Playback booking is conditional and shows the live booking SQL");
Check((await playback.FindNextAsync("Cardiology", true)).Slots.All(s => s.SlotId != network.Slots[0].SlotId || s.ProviderKey != "ridgeview"),
    "A booked playback slot leaves availability");

Console.WriteLine($"PASS: {checks} query-contract and component-rendering checks (no SQL connections).");
if (args.Contains("--live"))
{
    Console.WriteLine("LIVE: will permanently book one synthetic remote slot; requires approved pooled test setup.");
    var networkCheck = await sql.CheckNetworkAsync();
    Console.WriteLine($"Network check from {networkCheck.SourceDatabase}: " + string.Join("; ", networkCheck.Providers.Select(p => $"{p.Database} {p.OpenSlots} open")));
    var before = await sql.FindNextAsync("Cardiology", true);
    var slot = before.Slots.FirstOrDefault(s => s.ProviderKey == "ridgeview")
        ?? throw new InvalidOperationException("Seed an available Ridgeview cardiology slot before the live test.");
    var attempts = await Task.WhenAll(sql.BookAsync(slot.ProviderKey, slot.SlotId), sql.BookAsync(slot.ProviderKey, slot.SlotId));
    Check(attempts.Count(r => r.Booked) == 1, "Exactly one concurrent booking wins");
    var after = await sql.FindNextAsync("Cardiology", true);
    Check(!after.Slots.Any(s => s.ProviderKey == slot.ProviderKey && s.SlotId == slot.SlotId), "Booked slot disappears from availability");
    Console.WriteLine($"PASS: live cross-db search/concurrent booking, {slot.ProviderKey} slot {slot.SlotId}. This slot remains booked.");
}

sealed class CheckNavigation : NavigationManager
{
    public CheckNavigation() => Initialize("http://localhost/", "http://localhost/");
    protected override void NavigateToCore(string uri, bool forceLoad) =>
        throw new NotSupportedException("Rendering checks do not navigate.");
}

sealed class TestCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken) =>
        new("offline-test-token", DateTimeOffset.UtcNow.AddHours(1));
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken) =>
        ValueTask.FromResult(GetToken(context, cancellationToken));
}

sealed class MonitorHandler(string cpu, string billed, string memory) : HttpMessageHandler
{
    public int Capacity { get; set; } = 8;
    public bool Deny { get; set; }
    public List<string> Requests { get; } = [];
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!.AbsoluteUri;
        Requests.Add(uri);
        if (request.Headers.Authorization?.Scheme != "Bearer") throw new Exception("Missing bearer authorization");
        var content = uri.Contains("metricnames=app_cpu_percent") ? cpu
            : uri.Contains("metricnames=app_cpu_billed") ? billed
            : uri.Contains("metricnames=app_memory_percent") ? memory
            : JsonSerializer.Serialize(new { sku = new { name = "HS_S_Gen5", capacity = Capacity },
                properties = new { status = "Online", minCapacity = 1, elasticPoolId = (string?)null } });
        return Task.FromResult(new HttpResponseMessage(Deny ? HttpStatusCode.Forbidden : HttpStatusCode.OK)
        {
            Content = new StringContent(Deny ? "Monitoring permission denied" : content)
        });
    }
}
