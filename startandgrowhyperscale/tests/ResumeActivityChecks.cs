using System.Reflection;
using CaldovaNetworkScheduling.Components.Pages;
using CaldovaNetworkScheduling.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

internal static class ResumeActivityChecks
{
    public static async Task RunAsync(Action<bool, string> check, IConfiguration configuration)
    {
        await CheckRunControlsAsync(check, configuration);
        foreach (var scenario in new[] { "enabled", "fresh", "disabled", "failed" })
        {
            var signIn = new SignIn(configuration) { Fail = scenario == "failed" };
            var workload = new Workload(configuration, signIn) { Allow = scenario != "disabled" };
            var page = new Scale();
            var monitor = new ServerlessMonitor(new HttpClient(new MonitorHandler("{}", "{}", "{}")),
                new TestCredential(), configuration);
            Set(page, "Monitor", monitor);
            Set(page, "Resume", signIn);
            Set(page, "Workload", workload);
            Set(page, "Telemetry", new Telemetry(configuration));
            Set(page, "Logger", NullLogger<Scale>.Instance);
            var oldSample = new ServerlessCpuSample(DateTimeOffset.UtcNow.AddHours(-2), 50, 8);
            if (scenario != "fresh")
            {
                Field("samples").SetValue(page, new[] { oldSample });
                Field("runStart").SetValue(page, oldSample.At.AddMinutes(-1));
                Field("elapsedStart").SetValue(page, oldSample.At.AddMinutes(-1));
            }
            var lifetime = (CancellationTokenSource)Field("lifetime").GetValue(page)!;
            try
            {
                check(workload.Starts == 0, "Opening the page does not launch resume activity");
                await (Task)typeof(Scale).GetMethod("ResumeAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, null)!;
                var elapsedStart = (DateTimeOffset)Field("elapsedStart").GetValue(page)!;
                check(scenario == "fresh" ? elapsedStart > DateTimeOffset.UtcNow.AddSeconds(-5)
                    : elapsedStart == oldSample.At.AddMinutes(-1),
                    "Resume preserves elapsed time from Run, or starts at sign-in on a fresh page");
                if (scenario is "enabled" or "fresh")
                {
                    check(signIn.Succeeded && workload.Starts == 1 && workload.Workers == 4
                        && workload.Duration == TimeSpan.FromSeconds(30),
                        "A successful sign-in starts exactly four workers for 30 seconds");
                    var samples = (IReadOnlyList<ServerlessCpuSample>)Field("samples").GetValue(page)!;
                    check(scenario == "fresh" ? samples.Count == 2 :
                        samples.Count == 3 && samples[0] == oldSample && samples[1].At > oldSample.At.AddHours(1),
                        "Resume observation preserves the earlier chart and the real gap without filling it");
                    check(samples[^2].CpuPercent == 0 && samples[^1].CpuPercent > 0
                        && samples[^1].At - samples[^2].At == TimeSpan.FromSeconds(15),
                        "A real baseline and rising CPU sample form a visible segment for the short resume burst");
                    if (scenario == "fresh")
                        check((TimeSpan)Field("planned").GetValue(page)! == TimeSpan.FromSeconds(135)
                            && (DateTimeOffset)Field("runStart").GetValue(page)! <= DateTimeOffset.UtcNow.AddSeconds(-30),
                            "A fresh resume chart includes the real baseline and uses the short activity timeline");
                    check(Field("phase").GetValue(page)!.ToString() == "Load", "Resume activity enters the normal measured load phase");
                    typeof(Scale).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, null);
                    var segments = (System.Collections.IEnumerable)typeof(Scale)
                        .GetMethod("Segments", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, null)!;
                    check(segments.Cast<Array>().Any(segment => segment.Length >= 2),
                        "The actual chart emits a drawable segment during post-resume observation");
                    var rawSamples = new[] { samples[^2], samples[^1] with { CpuPercent = 3.25 } };
                    Field("samples").SetValue(page, rawSamples);
                    typeof(Scale).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, null);
                    var readout = typeof(Scale).GetProperty("Readout", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page);
                    check((string)readout! == "0.50", "CPU readout rounds 0.26 up to the next quarter vCore");
                    Field("resumeActivity").SetValue(page, false);
                    check((string)typeof(Scale).GetProperty("Readout", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)! == "0.50",
                        "The same quarter-vCore readout applies to the main Run, not only resume activity");
                    var roundedSegments = (System.Collections.Generic.IEnumerable<CaldovaNetworkScheduling.Models.ScaleSample[]>)
                        typeof(Scale).GetMethod("Segments", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, null)!;
                    check(roundedSegments.Last()[^1].VCores == 0.5
                        && (double)typeof(Scale).GetProperty("DisplayVCores", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(page)! == 0.5
                        && rawSamples[^1].UsedVCores == 0.26,
                        "Readout, gauge, and chart agree while the original CPU measurements remain unchanged");
                }
                else
                {
                    check(workload.Starts == 0, $"{scenario} sign-in never starts SQL activity");
                    if (scenario == "failed")
                        check(Field("error").GetValue(page) is string, "Failed sign-in displays an error");
                }
            }
            finally
            {
                lifetime.Cancel();
                if (Field("runTask").GetValue(page) is Task task) await task;
                (Field("run").GetValue(page) as CancellationTokenSource)?.Dispose();
                lifetime.Dispose();
            }
            check(!workload.Snapshot.Running, "Disposing the page stops the bounded resume workload");
        }
        var round = typeof(Scale).GetMethod("RoundVCores", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var (value, expected) in new[] { (0.0, 0.0), (0.01, 0.25), (0.25, 0.25), (0.26, 0.5),
            (0.5, 0.5), (0.51, 0.75), (0.75, 0.75), (0.76, 1.0), (1.0, 1.0), (3.336, 3.5), (8.0, 8.0) })
            check((double)round.Invoke(null, [value])! == expected,
                $"Quarter-vCore rounding preserves zero and exact boundaries: {value} -> {expected}");
        var formatElapsed = typeof(Scale).GetMethod("FormatElapsed", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var (seconds, expected) in new[] { (0.0, "00:00:00"), (65.9, "00:01:05"),
            (3661.0, "01:01:01"), (90061.0, "25:01:01"), (-30.0, "-00:00:30") })
            check((string)formatElapsed.Invoke(null, [seconds])! == expected,
                $"Elapsed formatting handles seconds, long gaps, and pre-sign-in samples: {expected}");
        var clockPage = new Scale();
        var origin = new DateTimeOffset(2026, 10, 28, 23, 59, 50, TimeSpan.Zero);
        Field("elapsedStart").SetValue(clockPage, origin);
        var elapsedAt = typeof(Scale).GetMethod("ElapsedAt", BindingFlags.NonPublic | BindingFlags.Instance)!;
        check((string)elapsedAt.Invoke(clockPage, [origin.AddSeconds(20)])! == "00:00:20"
            && (string)elapsedAt.Invoke(clockPage, [origin.AddSeconds(-30)])! == "-00:00:30",
            "Sample timestamps use the run origin across midnight without changing or clipping baseline times");
        var scene = new MeasuredClinicScene();
        scene.Update(origin, 1, 0);
        scene.Update(origin.AddSeconds(20), 1, 1);
        var snapshot = scene.Snapshot(true, false, origin.AddSeconds(20), origin, TimeSpan.FromMinutes(5),
            [], null, "");
        var activity = snapshot.Activity.Single();
        check(activity.RecordedAt == origin.AddSeconds(20)
            && (string)typeof(Scale).GetMethod("ActivityTime", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(clockPage, [activity])! == "00:00:20",
            "Schedule event times retain their UTC date internally but display elapsed time from Run");
        typeof(Scale).GetMethod("ResetMeasured", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(clockPage, null);
        check(Field("elapsedStart").GetValue(clockPage) is null
            && (string)typeof(Scale).GetProperty("ElapsedTime", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(clockPage)! == "00:00:00",
            "Reset returns the elapsed clock to zero");
        ((CancellationTokenSource)Field("lifetime").GetValue(clockPage)!).Dispose();
    }

    private static async Task CheckRunControlsAsync(Action<bool, string> check, IConfiguration configuration)
    {
        var pendingPage = new Scale();
        Field("phase").SetValue(pendingPage, Enum.Parse(Field("phase").FieldType, "Starting"));
        typeof(Scale).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pendingPage, null);
        var readout = typeof(Scale).GetProperty("Readout", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var awaitingCpu = typeof(Scale).GetProperty("AwaitingCpu", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var display = typeof(Scale).GetProperty("DisplayVCores", BindingFlags.NonPublic | BindingFlags.Instance)!;
        check((string)readout.GetValue(pendingPage)! == "1.00" && (bool)awaitingCpu.GetValue(pendingPage)!
            && (double)display.GetValue(pendingPage)! == 1,
            "Run shows the configured 1-vCore minimum immediately in the readout and gauge while CPU is pending");
        var segments = (System.Collections.IEnumerable)typeof(Scale).GetMethod("Segments",
            BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pendingPage, null)!;
        check(!segments.Cast<Array>().Any()
            && ((IReadOnlyList<ServerlessCpuSample>)Field("samples").GetValue(pendingPage)!).Count == 0,
            "The pending minimum creates no CPU samples or chart points");
        Field("samples").SetValue(pendingPage, new[] { new ServerlessCpuSample(DateTimeOffset.UtcNow, 3.25, 8) });
        typeof(Scale).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pendingPage, null);
        check((string)readout.GetValue(pendingPage)! == "0.50" && !(bool)awaitingCpu.GetValue(pendingPage)!,
            "The first real sample replaces the pending minimum with rounded measured CPU");
        Field("samples").SetValue(pendingPage, new[] { new ServerlessCpuSample(DateTimeOffset.UtcNow, 0, 8) });
        typeof(Scale).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pendingPage, null);
        check((string)readout.GetValue(pendingPage)! == "0.00" && (double)display.GetValue(pendingPage)! == 0,
            "A real zero is never replaced with the configured minimum");
        typeof(Scale).GetMethod("ResetMeasured", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pendingPage, null);
        typeof(Scale).GetMethod("Refresh", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(pendingPage, null);
        check((string)readout.GetValue(pendingPage)! == "–" && !(bool)awaitingCpu.GetValue(pendingPage)!,
            "An idle page still shows no CPU measurement instead of a minimum placeholder");
        ((CancellationTokenSource)Field("lifetime").GetValue(pendingPage)!).Dispose();
        check((TimeSpan)typeof(Scale).GetField("SamplePollInterval", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)! == TimeSpan.FromSeconds(3),
            "The app checks for newly published 15-second SQL samples every three seconds");
        var workload = new RampWorkload(configuration);
        var page = new Scale();
        Set(page, "Monitor", new ServerlessMonitor(new HttpClient(new MonitorHandler("{}", "{}", "{}")),
            new TestCredential(), configuration));
        Set(page, "Telemetry", new Telemetry(configuration));
        Set(page, "Workload", workload);
        Set(page, "Logger", NullLogger<Scale>.Instance);
        var lifetime = (CancellationTokenSource)Field("lifetime").GetValue(page)!;
        try
        {
            typeof(Scale).GetMethod("Run", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(page, null);
            var started = await Task.WhenAny(workload.Started.Task, Task.Delay(TimeSpan.FromSeconds(2)));
            check(started == workload.Started.Task && workload.Snapshot.Active == 1,
                "The actual Run handler starts its first worker without the former 30-second baseline");
        }
        finally
        {
            lifetime.Cancel();
            if (Field("runTask").GetValue(page) is Task task) await task;
            if (Field("polling").GetValue(page) is Task polling) await polling;
            (Field("run").GetValue(page) as CancellationTokenSource)?.Dispose();
            lifetime.Dispose();
        }
        foreach (var phase in new[] { "Starting", "Load", "Cooldown" })
        {
            var stoppedPage = new Scale();
            var stoppedWorkload = new RampWorkload(configuration);
            var run = new CancellationTokenSource();
            var sample = new ServerlessCpuSample(DateTimeOffset.UtcNow, 25, 8);
            var retainedSamples = new[] { sample };
            Set(stoppedPage, "Workload", stoppedWorkload);
            Field("run").SetValue(stoppedPage, run);
            Field("phase").SetValue(stoppedPage, Enum.Parse(Field("phase").FieldType, phase));
            Field("resumeActivity").SetValue(stoppedPage, phase == "Load");
            Field("samples").SetValue(stoppedPage, retainedSamples);
            var scene = (MeasuredClinicScene)Field("scene").GetValue(stoppedPage)!;
            scene.Update(DateTimeOffset.UtcNow, 1, 0);
            var polling = WaitForCancellationAsync(run.Token);
            var runTask = WaitForCancellationAsync(run.Token);
            Field("polling").SetValue(stoppedPage, polling);
            Field("runTask").SetValue(stoppedPage, runTask);
            try
            {
                var stop = (Task)typeof(Scale).GetMethod("StopAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(stoppedPage, null)!;
                await stop.WaitAsync(TimeSpan.FromSeconds(2));
                check(run.IsCancellationRequested && polling.IsCompleted && runTask.IsCompleted,
                    $"Stop during {phase} cancels and joins observation and orchestration without a cooldown delay");
                check(Field("phase").GetValue(stoppedPage)!.ToString() == "Stopped"
                    && !(bool)Field("resumeActivity").GetValue(stoppedPage)!
                    && !(bool)typeof(Scale).GetProperty("Busy", BindingFlags.NonPublic | BindingFlags.Instance)!
                        .GetValue(stoppedPage)!
                    && !stoppedWorkload.Snapshot.Running,
                    $"Stop during {phase} returns controls to idle and stops all workers");
                check(ReferenceEquals(Field("samples").GetValue(stoppedPage), retainedSamples)
                    && scene.Snapshot(false, false, DateTimeOffset.UtcNow, null, TimeSpan.FromMinutes(4.5),
                        retainedSamples, null, "").ActiveUsers == 0,
                    $"Stop during {phase} clears online staff without erasing measured samples");
            }
            finally
            {
                run.Cancel();
                await polling;
                await runTask;
                run.Dispose();
                ((CancellationTokenSource)Field("lifetime").GetValue(stoppedPage)!).Dispose();
            }
        }
    }

    private static async Task WaitForCancellationAsync(CancellationToken token)
    {
        try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private static FieldInfo Field(string name) =>
        typeof(Scale).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static void Set(Scale page, string name, object value) =>
        typeof(Scale).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(page, value);

    private sealed class SignIn(IConfiguration configuration)
        : ServerlessResume(configuration, NullLogger<ServerlessResume>.Instance)
    {
        public bool Fail { get; init; }
        public bool Succeeded { get; private set; }
        public override Task<ResumeResult> SignInAsync(ServerlessConfiguration database, Action<int>? onAttempt, CancellationToken token)
        {
            if (Fail) throw new InvalidOperationException("Test sign-in failure");
            Succeeded = true;
            return Task.FromResult(new ResumeResult(TimeSpan.FromSeconds(15), 1, DateTimeOffset.UtcNow));
        }
    }

    private sealed class Workload(IConfiguration configuration, SignIn signIn)
        : ServerlessSqlWorkload(configuration, NullLogger<ServerlessSqlWorkload>.Instance)
    {
        public bool Allow { get; init; }
        public int Starts { get; private set; }
        public int Workers { get; private set; }
        public TimeSpan Duration { get; private set; }
        private SqlWorkloadSnapshot state = new(false, 0, 0, 0, null, null, null);
        public override bool Enabled => Allow;
        public override SqlWorkloadSnapshot Snapshot => state;
        public override void Start(ServerlessConfiguration database, int workers, TimeSpan duration)
        {
            if (!signIn.Succeeded) throw new Exception("Workload began before successful sign-in");
            Starts++;
            Workers = workers;
            Duration = duration;
            state = new(true, workers, 3, 0, DateTimeOffset.UtcNow, null, null, workers);
        }
        public override Task StopAsync()
        {
            state = state with { Running = false, Active = 0 };
            return Task.CompletedTask;
        }
    }

    private sealed class Telemetry(IConfiguration configuration) : ServerlessSqlTelemetry(configuration)
    {
        public override Task<IReadOnlyList<ServerlessCpuSample>> ReadAsync(
            ServerlessConfiguration database, DateTimeOffset from, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<ServerlessCpuSample>>([new(from, 0, 8), new(from.AddSeconds(15), 25, 8)]);
    }

    private sealed class RampWorkload(IConfiguration configuration)
        : ServerlessSqlWorkload(configuration, NullLogger<ServerlessSqlWorkload>.Instance)
    {
        public TaskCompletionSource<WorkloadRamp> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private SqlWorkloadSnapshot state = new(true, 8, 0, 0, DateTimeOffset.UtcNow, null, null, 1);
        public override bool Enabled => true;
        public override SqlWorkloadSnapshot Snapshot => state;
        public override void StartRamp(ServerlessConfiguration database, WorkloadRamp ramp) => Started.SetResult(ramp);
        public override Task StopAsync()
        {
            state = state with { Running = false, Active = 0 };
            return Task.CompletedTask;
        }
    }
}
