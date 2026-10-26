# Caldova - Start and Grow Hyperscale

Two independently presentable chapters tell the story. Use the
[Start and Grow skill](../.github/skills/start-and-grow-hyperscale-demo/SKILL.md)
to build/verify the demo or cue one beat at a time. For the take-by-take
recording order and current status, see [RECORDING-SCRIPT.md](RECORDING-SCRIPT.md). Stop between beats and at
the chapter boundary. "Next" is not approval to change Azure.

The current app still has four numbered pages. The chapter split below is the
agreed presentation flow, not a claim that new app navigation or scaling
controls have already been implemented.

## Readiness gates

| Capability | Current source baseline / required proof |
|---|---|
| Copilot sizing/deployment | Skill interviews, recommends serverless Hyperscale 1-8 vCores, and deploys the Part 1 server/database with `deploy\09-deploy-part1-sql.ps1` (`-WhatIf` plan, then apply). Script plan verified 2026-10-24; not yet run on camera. |
| Independent chapter navigation | Not yet implemented; existing routes are mapped below. |
| Serverless Patient Access | Published illustration passed the Edge 1 -> 4 -> 1 -> pause -> resume cycle. Current app uses SQL DMV CPU samples and a staged 0 -> 8 -> 0 worker ramp; published 2026-10-23 with load generation disabled. Harness ramp measured 0 -> 6.94 -> 0 vCores with no failures (6-7 accepted); app-page ramp not yet run live. Actual billing/pause/resume history is captured; portal rehearsal and deployed SQL identity verification remain. SQL load generation is disabled. |
| Pool membership | Canonical script/skill implemented; verify current live state before each run. |
| Pool scale under workload | Not yet implemented or measured. Requires an approved target, bounded workload, and before/during/after evidence. Retry handling is a talking point only, not an implementation or verification gate. |
| Database growth | Not yet implemented. Add bounded approved synthetic inserts and measured data-space-used growth in the workload flow, not a separate storage/I/O checkpoint page. |
| Native cross-database search/booking | Source implemented; live deployment/app-identity verification remains unproven by this document. |

Measured historical evidence is in [README.md](README.md). It is not
current Azure state. Stop a live beat if its gate has not passed; do not present
planned capabilities as working.

## Preparation

- **One GitHub Copilot chat session for the whole demo.** Every on-camera Copilot
  step (Beat 1.2 sizing/deployment, Beat 2.1 pool integration, and the reset)
  runs in that same chat, driven by the single
  [start-and-grow-hyperscale-demo skill](../.github/skills/start-and-grow-hyperscale-demo/SKILL.md).
  The presenting machine must be signed in with `az login` to the demo subscription.
- Start with the three databases standalone for the serverless chapter.
- Run the scheduling SQL setup in [README.md](README.md#sql-setup) before presenting.
  The native view requires all three databases in the enabled pool during setup.
  After setup, they can be returned to standalone; the view cannot be queried
  across databases again until pooled placement is restored.
- Publish the updated app only after checking it locally.
- Current source uses SQL DMV CPU data and opt-in real SQL load; billing remains
  a separate Azure Monitor portal visual. SQL observation prevents auto-pause:
  stop observation and all SQL clients before the pause beat. The DMV revision
  was published 2026-10-23; the existing illustration remains unchanged.
  Search and booking execute SQL over synthetic schedules, with no scripted fallback.
- Use the app's managed identity for the deployed read/write verification.

## Part 1 - Start with Hyperscale

### Beat 1.1 - Azure Portal choices

**Say:** "Normally I need to choose the database configuration here. If I don't
know what I need to deploy, that's OK."

**Do:** Open the Azure SQL database creation experience without submitting it.
Show the actual choices: service tier, compute model/range, region,
availability/redundancy, and connectivity. Verify the current portal labels
during rehearsal; do not leave "XYZ" in the on-camera script.

**Prove:** Distinguish choices the customer must make from what the platform
manages. This beat creates nothing.

### Beat 1.2 - Copilot-assisted sizing and deployment

**Say:** "That's a lot of choices. Instead, I'll switch to VS Code. GitHub Copilot
uses our deployment skill to ask about my requirements, recommend a
configuration, and deploy it with scripts and the Azure CLI."

**Do:** Show the workspace
[skill](../.github/skills/start-and-grow-hyperscale-demo/SKILL.md).
Use this prompt:

> Help me size and deploy a Hyperscale database for Caldova Patient Access.
> Ask me for the missing requirements, recommend a configuration, and show me
> a change summary. Don't deploy until I approve the exact plan.

Answer Copilot's questions with these requirements (say them in your own words):

| Topic | Requirement to say |
|---|---|
| Workload | Clinic staff sign in to schedule and check in patients. Busy weekday mornings, light in the afternoon. |
| Peak | We don't know the exact peak yet. It should scale up on its own for the morning rush; we don't want to pay for peak all day. |
| Idle | Nights and weekends nobody uses it. |
| Warm-up | A short wait for the first sign-in after a quiet period is fine. |
| Data | Starts small, grows as we add clinics. I don't want to size storage. |
| Region | East US 2. |
| Availability | Pilot: no zone redundancy, local backup redundancy. |
| Security | Microsoft Entra only; no SQL passwords. |
| Budget | Lowest possible cost when idle. |

Copilot's recommendation, and why:

| Choice | Value | Reason |
|---|---|---|
| Service tier | Hyperscale | Storage grows automatically; no size to pick. Can join a Hyperscale elastic pool later. |
| Compute | Serverless, `HS_S_Gen5_8` | Intermittent use; billed per second for what is used. |
| vCore range | 1-8 | Max 8 is headroom for an unknown morning peak. Min 1 is the lowest minimum Azure allows with a max of 8 (verified with the regional capability API), so idle-but-online cost is as low as possible. |
| Auto-pause | 60 minutes | Nights and weekends. 60 minutes is the Hyperscale minimum; Hyperscale auto-pause is preview. |
| Redundancy | Local backups, no zone redundancy | Pilot requirement; also matches the pool Caldova joins in Part 2. |
| Auth | Entra-only | Requirement. |

Copilot then shows the change summary by running:

```powershell
.\startandgrowhyperscale\deploy\09-deploy-part1-sql.ps1 -WhatIf
```

After you approve, it runs the same command without `-WhatIf` (add `-ClientIp <presenter-ip>`
and, on an Azure-hosted presenting machine, `-AllowAzureServices`). It creates
`sql-caldova-start-0efc44` and `caldova-scheduling`, then verifies the result.
Creation takes a few minutes; time-lapse with disclosure.

**Prove:** The verified result matches the approved plan (`HS_S_Gen5_8`, min 1,
auto-pause 60). Do not claim the 1-8 range came from measurement; it is a
starting point that Beat 1.3 then observes. Verify that the intended GitHub
branch contains the skill before showing its repository URL on camera.

### Beat 1.3 - Caldova Patient Access (current app Step 1, `/`)

**Presenter decision (2026-10-22): two complementary visuals.**
The app uses a **staged ramp** (presenter decision 2026-10-23): 0 -> 8 -> 0
workers, one per 30 seconds with a 60-second hold, 8 minutes total; a
**6-7 CPU-used vCore** peak is acceptable. Current source measures
approximately 15-second `sys.dm_db_resource_stats` interval averages using
`avg_cpu_percent * cpu_limit / 100`, not Azure Monitor CPU data.
The approved harness ramp from an Azure client (2026-10-23 00:45-00:54 UTC)
rose smoothly to a **6.94-vCore** peak and back to 0 with no failures. The
app-page ramp has not been run against SQL. The DMV/ramp revision was
published 2026-10-23 with load generation still disabled. The approved
fallback at `/serverless-illustration` explicitly illustrates 1 -> 4 -> 1 ->
paused -> resumed. Its complete cycle has been verified in standalone Edge.
Never describe that curve as actual SQL telemetry.
Use the published scene at
<https://app-caldova-network-0efc44.azurewebsites.net/serverless-illustration>.
The measured page and illustration are separate routes of the same app;
only one presentation window is needed.

In the portal, billed compute is the story. Do not spend the presentation
comparing CPU with memory or hovering over billing numbers. Keep the chart
accurately labeled and let the bars show:

1. Quiet but online: average billed compute near the configured **1-vCore minimum**.
2. Activity: billed compute rises above that minimum.
3. Activity stops: billed compute falls back toward the minimum.
4. Confirmed auto-pause: **no compute charge** during the paused interval.
5. An approved application connection resumes the database: billed compute returns.

**Gate:** Verify the complete real billing/pause/resume timeline before presenting.
Earlier tests prove billed rise/fall. Historical Activity Log and billing now
also confirm auto-pause at **2026-10-21 20:54:22 UTC** and resume at
**2026-10-22 16:01:41 UTC**, with absent billing samples during the confirmed
paused interval and samples returning afterward. These are separate historical
views, not a newly recorded single synchronized app-and-portal session.
Approve monitoring permissions and real compute usage, restrict app access, and
enable load only for an approved run. The 0 -> 8 -> 0 ramp is measured in the
harness only; the published app contains it but load generation is disabled. The labeled
illustration is verified.
No workload-memory savings claim is needed.

**Chart units:** `app_cpu_billed` supports **Sum**, in vCore-seconds. To show
average billed vCores, divide each bucket's Sum by its duration in seconds
(60 for a one-minute bucket), using an Azure Monitor workbook transformation
and bar visualization. Alternatively, use the raw Metrics bar chart with its
actual units: **60 vCore-seconds per full minute corresponds to 1 average vCore**.
Do not relabel raw Sum as vCores or select an unsupported Average aggregation.

For auto-pause, stop all SQL workloads and close connections, including query
editors, SQL health checks, and other clients. Allow the configured 60-minute
idle delay plus the actual transition time; do not promise an exact pause time.
Observe status/activity through ARM/portal, not SQL probes. Record the confirmed
Paused interval and the later resume event. A missing metric bar alone is not
proof of pause or zero charge; label the confirmed paused/no-compute-charge
interval without fabricating zero-valued metric samples. Record/time-lapse if
needed and retain real timestamps. Resuming requires explicit approval for
the connection/compute use, not a fabricated animation.

After the DMV revision is separately approved, published, and verified with the
application identity, click **Observe SQL CPU**. Capture quiet SQL intervals.
Start the approved bounded workload, then stop it and continue observing.
Chart real, approximately 15-second interval averages with UTC end timestamps.
Stop **observation** before the auto-pause beat. There is no scripted patient activity,
pause/resume, or guaranteed duration/curve. Record and time-lapse if necessary;
label the recording and preserve real timestamps.

In the Azure portal, open the Caldova database for the separate billed-compute
bar chart. In **Monitoring > Metrics**, select `app_cpu_billed`, **Sum**, with
the intended historical or newly recorded range. For one-minute buckets,
vCore-seconds / 60 gives average billed vCores in a workbook. Show bars without
hovering; do not use Azure Monitor CPU values to drive or validate the app chart.

The app shows raw SQL CPU percentages and each sample's own vCore limit beside
the converted CPU-used values. Portal billing is a different metric and interval.
These are consumption/billing, not allocated-core measurements. Missing samples
alone do not prove zero charges.

Explain per-second CPU/memory-based compute billing with the configured minimum:
idle but online still incurs compute charges; paused compute costs nothing.
Storage and applicable backup charges continue while paused. If already pooled,
restore standalone compute only with approval; the measured serverless app
rejects pooled/provisioned targets instead of plotting misleading data.

Explain that demand returning may require compute and caches to warm back up.
The chart does not directly measure cache reclamation, resume duration, or
retry behavior. Retry handling remains a talking point, not a new implementation.

**End Part 1 here.** Stop SQL work and observation. Start Part 2 only when Bob
requests it; the serverless page no longer automatically advances to acquisitions.

### Explicit illustration fallback

If live timing is unsuitable, stop the measured workload/observation and choose
**Scripted illustration (fallback)**, or navigate to `/serverless-illustration`.
Do not silently switch on telemetry failure. The page labels staff, activities,
cores, clock, and pause/resume as scripted. It runs no SQL and supplies no billing
evidence. Say: "This illustrates the behavior; now let's look at actual usage
and billing from a recorded workload."

The real portal billing chart is mandatory even with this fallback. Use a disclosed
historical window; it need not match the scripted scene. An initial usable window
is **2026-10-22 16:35-16:43 UTC** on the actual Caldova database. Use one-minute
App CPU billed **Sum**, converted to average billed vCores in a workbook or shown
with explicit raw units. CPU/memory comparison is optional diagnostic detail,
not a presentation requirement. This earlier window shows rise/fall only; capture
a longer confirmed pause/resume view as documented below. The
[README live result](README.md#live-result---2026-10-22) records observed values.
Confirm chart rendering/time range during rehearsal; API samples alone do not
prove the portal/workbook presentation has been rehearsed.

**Historical pause/resume view:** set the portal to **2026-10-21 19:45 UTC through
2026-10-22 16:45 UTC** (October 21 14:45 through October 22 11:45 CDT), using
15-minute App CPU billed Sum bars. Quiet full buckets before pause are around
900 vCore-seconds (1 average billed vCore). The last pre-pause bucket is partial;
the confirmed paused interval has no samples. Billing returns after resume;
zoom to **16:35-16:43 UTC on October 22** for the stronger rise/fall view.
No hover narration or on-stage calculation is required. No portal screenshot
or saved workbook is claimed verified merely from these API results.

## Part 2 - Grow without redesigning

Preflight this chapter independently: confirm all three database configurations,
the existing preview-enabled pool, SQL setup, and app identity. Use the
standalone baseline for the full migration story. If already pooled, disclose
that state; restoration needs its own approval. Before presenting the full
chapter, the scaling/workload gate above must pass.

### Beat 2.1 - Bring the acquired providers together (current app Step 2, `/network`)

Show Caldova, Northbrook Family Clinics, and Ridgeview Specialty Care. The cards
describe their starting configurations, not live Azure state.

Switch to the same GitHub Copilot chat used for Beat 1.2 and type:

> Integrate Caldova's acquired providers into the scheduling pool.

The start-and-grow skill's pool membership procedure previews the exact subscription/resources
and requests approval. Ensure no scheduling/workload traffic is active before
approving. It adds databases to the existing preview-enabled pool, not a new one.
Caldova converts from serverless to provisioned compute. Wait for all three
postconditions to pass.

### Beat 2.2 - One pool, one network (current app Step 3 Network, `/verify`)

In the Azure portal, open the pool directly:

1. Resource group `caldova-network-rg`.
2. Server `sql-caldova-network-0efc44`.
3. Hyperscale pool `caldova-network-pool`.
4. Confirm all three scheduling databases are pooled and online.
5. Inspect actual pool capacity; no app-generated metrics are shown.

Then press **Check network** in the app. From Caldova it runs the native join
view using the configured application identity and shows each database's open
slots and the query. It does not prove booking permissions. Failures are displayed
and logged; do not continue as if SQL passed.

Present the scaling and database-growth beats before moving to Search & Book.

### Beat 2.3 - Scale the pool while watching workload

**Gate:** This beat is not implemented yet. Build and verify the scale operation
and workload observation before presenting it live. Retry handling is a talking
point only; do not build retry logic, instrumentation, or tests for this demo.

**Say:** "The service prepares the new compute and switches over when it's ready.
This is an online operation, but the application can see a brief interruption
at the end, so it needs connection retry handling."

**Do:** Record the existing capacity and approved target on the same pool.
Start the bounded workload and capture a baseline. Apply the approved scale
operation while keeping workload active. Observe operation state alongside
timestamped successful requests, latency, SQL/connection errors, and observed
workload recovery. Continue observation after ARM completion. Explain the need
for connection retry handling without claiming the app demonstrates it.

**Prove:** Actual before/after capacity and application behavior over the same
timeline. ARM success alone does not prove application recovery. Report any
observed interruption duration; if none is observed, say that rather than
claim "zero interruption." A retry demonstration is not required. Preserve
existing booking behavior; never blindly retry a booking whose write outcome
is unknown.

### Beat 2.4 - Show the database growing in the workload flow

**Gate:** The current serverless read-only workload does not grow data. This
separate bounded insert/space-measurement path is not yet implemented.

**Say:** "We don't size database storage by choosing a core count. Hyperscale
handles storage growth separately; there's no storage tier to resize here."

**Do:** Within the workload screen, run approved bounded synthetic inserts while
holding compute fixed. Show measured database data space used increasing.
No separate storage/I/O checkpoint page is needed.

**Prove:** Scaling compute does not require a matching manual database-storage
resize. Do not imply unlimited I/O, that all I/O limits are independent of
compute, or that storage grew during the demo without measured evidence. Agree the
volume/cost and scoped cleanup beforehand. No fake storage-growth gauge.

### Beat 2.5 - Search the network, book at the provider (current app Step 4, `/appointments`)

1. Search Cardiology with **Search the entire network** unchecked.
2. Check the box and search again. Compare the returned lead time to Caldova only.
   Dates/counts are actual seeded SQL results, not fixed claims.
3. Expand **Behind the view**. Caldova's central provider/clinic directory joins
   to local and native three-part-name remote appointment tables. `UNION ALL`
   combines the joined provider results.
4. Book a Ridgeview slot. The displayed SQL runs one transaction from the Caldova
   connection: it updates the slot and inserts an appointment row, both in
   `[ridgeview-scheduling]`, a cross-database `INSERT` with one write database.
5. The slot disappears from open availability. Repeat the same slot from a
   second browser session to demonstrate that only the first booking can succeed.
6. Switch to Pediatrics to show Northbrook's schedule.

All displayed times are UTC. Booking records availability only; there is no
patient information in this synthetic scenario.

The transaction writes only to the provider's database (the preview allows one
write database per transaction). A competing booking returns unavailable, not
success. A lost write response is an unknown outcome: inspect that provider's slot
in SQL before retrying. No automatic write retry and no write to Caldova.

## Reset and chapter exit

End Part 2 after search/booking; do not automatically change Azure state.
If pool compute was scaled, restoring its previous capacity is a separate
approved operation. The membership reset below does not restore pool capacity.

Stop dependent SQL traffic. In the same Copilot chat, type:

> Return the providers to standalone and restore Caldova serverless.

The skill restores Caldova's 1-8 vCore serverless compute with 60-minute
auto-pause, Northbrook's 2 vCores, and Ridgeview's 4 vCores. It retains all data,
bookings, schema, grants, and the preview-enabled pool. Auto-pause is not immediate.
Native cross-database reads/writes will no longer work while standalone.

Stop observation before any compute reset. Starting a new observation clears
its local chart only; it does not reset Azure. There is no scripted
pool reset/integration button. Seed reruns do not reopen booked slots; they add
missing synthetic dates only.
