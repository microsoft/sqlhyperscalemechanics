using CaldovaNetworkScheduling.Models;

namespace CaldovaNetworkScheduling.Services;

/// <summary>
/// Projects a measured SQL run onto the Patient Access scene. Staff online equals the actual number of
/// active SQL workers; schedule entries are illustrative and advance only while SQL batches are completing.
/// vCores and the chart come only from SQL DMV samples, never from this class.
/// </summary>
public sealed class MeasuredClinicScene
{
    private readonly List<SimulatedUser> users = [];
    private readonly Dictionary<int, DateTimeOffset> nextActionAt = [];
    private readonly List<AppActivity> activity = [];
    private Random random = new(2026);
    private int nextUserId;
    private int sequence;
    private int booked;
    private int checkedIn;
    private int rescheduled;
    private int peak;
    private long lastCompleted;

    public MeasuredClinicScene() => Reset();

    public void Reset()
    {
        users.Clear();
        nextActionAt.Clear();
        activity.Clear();
        random = new Random(2026);
        nextUserId = 1;
        sequence = booked = checkedIn = rescheduled = peak = 0;
        lastCompleted = 0;
    }

    public void Update(DateTimeOffset now, int activeWorkers, long completedBatches)
    {
        while (users.Count < activeWorkers)
        {
            var id = nextUserId++;
            var seat = ClinicCast.Seats[(id - 1) % ClinicCast.Seats.Length];
            users.Add(new SimulatedUser(id, $"Staff {id:00}", seat.Role, seat.Location));
            nextActionAt[id] = now.AddSeconds(0.3 + random.NextDouble() * 1.2);
        }
        // The ramp stops the most recently started worker first.
        while (users.Count > activeWorkers)
        {
            nextActionAt.Remove(users[^1].Id);
            users.RemoveAt(users.Count - 1);
        }
        peak = Math.Max(peak, users.Count);

        var flowing = completedBatches > lastCompleted;
        lastCompleted = completedBatches;
        if (!flowing) return;
        foreach (var user in users)
        {
            if (now < nextActionAt[user.Id]) continue;
            Record(user, now);
            nextActionAt[user.Id] = now.AddSeconds(2.0 + random.NextDouble() * 2.5);
        }
    }

    public ScaleSnapshot Snapshot(bool running, bool complete, DateTimeOffset now, DateTimeOffset? start, TimeSpan planned,
        IReadOnlyList<ServerlessCpuSample> samples, ServerlessConfiguration? database, string statusDetail,
        ServerlessStatus? azureStatus = null)
    {
        var origin = start ?? now;
        var points = samples.Select(s => new ScaleSample((s.At - origin).TotalSeconds, 0, s.UsedVCores, ServerlessStatus.Active)).ToArray();
        var total = Math.Max(planned.TotalSeconds, points.Length == 0 ? 0 : points[^1].ScriptSeconds);
        return new ScaleSnapshot(
            IsRunning: running,
            IsComplete: complete,
            ScriptSeconds: start is null ? 0 : (now - origin).TotalSeconds,
            TotalScriptSeconds: Math.Max(15, total),
            SimulatedTime: TimeOnly.FromDateTime(now.UtcDateTime),
            Status: azureStatus ?? (users.Count > 0 ? ServerlessStatus.Active : ServerlessStatus.Online),
            StatusDetail: statusDetail,
            ActiveUsers: users.Count,
            PeakUsers: peak,
            VCores: samples.Count == 0 ? 0 : samples[^1].UsedVCores,
            MinVCores: database?.MinVCores ?? 1,
            MaxVCores: Math.Max(database?.MaxVCores ?? 8, samples.Count == 0 ? 0 : samples.Max(s => s.CpuLimit)),
            DatabaseName: database?.ResourceId.Split('/').Last() ?? "caldova-scheduling",
            ServiceObjective: "serverless",
            AutoPauseDelayMinutes: 0,
            Booked: booked,
            CheckedIn: checkedIn,
            Rescheduled: rescheduled,
            Samples: points,
            Users: users.ToArray(),
            Activity: Enumerable.Reverse(activity).Take(7).ToArray());
    }

    private void Record(SimulatedUser user, DateTimeOffset now)
    {
        var roll = random.NextDouble();
        var service = ClinicCast.Services[random.Next(ClinicCast.Services.Length)];
        var patient = ClinicCast.Patients[random.Next(ClinicCast.Patients.Length)];
        var at = TimeOnly.FromDateTime(now.UtcDateTime);
        string kind, when;
        if (roll < 0.6) { kind = "Booked"; booked++; when = ClinicCast.FutureSlot(random); }
        else if (roll < 0.85) { kind = "Checked in"; checkedIn++; when = $"Today {at:h:mm tt}"; }
        else { kind = "Rescheduled"; rescheduled++; when = ClinicCast.FutureSlot(random); }
        activity.Add(new AppActivity(++sequence, at, user.Id, kind, patient, service.Specialty, service.Clinic, when));
        if (activity.Count > 100) activity.RemoveRange(0, activity.Count - 100);
    }
}
