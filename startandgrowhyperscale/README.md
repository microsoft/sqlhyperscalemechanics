# Caldova Network Scheduling — Hyperscale Basics

Caldova Health's scheduling network on Azure SQL Database Hyperscale: **"Start here, stay here."**

Current readiness and the take-by-take recording order are in
[RECORDING-SCRIPT.md](RECORDING-SCRIPT.md). Measured Azure evidence is recorded below.

Caldova Health starts on one Hyperscale **serverless** database, then acquires two providers —
**Northbrook Family Clinics** and **Ridgeview Specialty Care** — and brings all three scheduling
databases into one **Hyperscale elastic pool**. Caldova staff can then find the next available
appointment anywhere in the network with one query, while storage grows without anyone declaring
files or sizes.

The app avoids the word "scheduler" for people (it collides with SQLOS schedulers); they are
**staff**.

## Two-part demo and guiding skill

Use [start-and-grow-hyperscale-demo](../.github/skills/start-and-grow-hyperscale-demo/SKILL.md)
to build, repair, verify, or present this demo one beat at a time.
The [runbook](DEMO-RUNBOOK.md) is the presentation source of truth:

1. **Start with Hyperscale:** Azure Portal choices, Copilot-assisted requirements/
   sizing/deployment and change summary, then real SQL load and actual Azure
   Monitor CPU-used/billed-compute measurements.
2. **Grow without redesigning:** move the three providers into the existing pool,
   verify placement, scale pool compute while observing workload behavior,
   discuss connection retry handling as a talking point only, show actual database
   growth within the workload flow, then native cross-database search and booking.

Example prompts: "Build the start and grow demo", "Verify the start and grow demo",
"Run the start and grow demo", or "Start the grow chapter".
Creating the skill does not deploy resources or change the app.
The app still has the four-page navigation below. Independent chapter navigation
and pool scaling under workload observation are **not yet implemented**.
The skill defines their build and evidence gates; it does not simulate success.
Retry logic, retry instrumentation, and retry tests are not part of the build.

> **Current source implementation:** Step 1 uses real Azure Monitor data and an
> opt-in bounded SQL read workload, not the former scripted scene. Live
> scale-up/down and telemetry delay have not yet been verified. Steps 2 and 3 guide real pool operations in Copilot and
> the Azure portal. Step 4 executes native cross-database reads and a single
> provider write over synthetic scheduling data. SQL setup and live verification
> are required before publishing; there is no scripted appointment fallback.

## Live app

### Published serverless illustration

The updated package is published to the existing App Service. Launch
<https://app-caldova-network-0efc44.azurewebsites.net/serverless-illustration>
for the approved, explicitly labeled scripted scene.
The measured page remains at `/`; these are two routes of one deployed app.
`Demo__Mode=Measured` is the startup compatibility setting, not a claim that
the illustration route uses real telemetry. Live SQL load generation is
explicitly disabled.

The deployed interactive scene passed an Edge browser check: start at 1.00,
rise to 4.00, fall to 1.00, scripted pause at zero, resume above zero, reset,
and return to the measured view. No browser errors were observed.
All six page/liveness endpoints returned HTTP 200, and 74 offline contract/
component checks passed. These checks do not establish application SQL access,
real four-vCore usage, or visual rehearsal of the authenticated Azure portal.
Historical billing and pause/resume evidence is documented below; the portal
bar chart still requires the presenter to set and verify the intended view.

| | |
|---|---|
| URL | <https://app-caldova-network-0efc44.azurewebsites.net> |
| Resource group | `caldova-network-rg` (subscription `AzureSQL_bobward`) |
| App Service | `app-caldova-network-0efc44` on Windows S1 plan `asp-caldova-network-0efc44`, East US 2 |
| Logical server | `sql-caldova-network-0efc44.database.windows.net` — **East US 2**, Microsoft Entra-only auth, public endpoint + firewall |
| Elastic pool | `caldova-network-pool` — Hyperscale, 8 vCores, on the same logical server; **empty** for now |
| Databases | `caldova-scheduling` (serverless `HS_S_Gen5_8`, 1–8 vCores, auto-pause 60 min), `northbrook-scheduling` (provisioned `HS_Gen5_2`), `ridgeview-scheduling` (provisioned `HS_Gen5_4`) — all standalone |

The table describes the original standalone baseline. Use `05-verify-sql.ps1`
or the Azure portal for current placement. Source changes are not automatically
published to the live app.

## Pages

The top navigation is numbered in story order.

| Nav | Beat | What it shows |
|---|---|---|
| **1 · Patient Access** (`/`) | Start here | Current source: explicitly start SQL DMV observation, then approved SQL load. Chart CPU used from 15-second SQL resource stats; billing is a separate portal visual. This DMV/ramp revision was published 2026-10-23 (load generation remains disabled). No generated patient activity or scripted scaling/pause/resume. |
| **2 · Acquisitions** (`/network`) | Bring providers together | Starting-configuration cards. **Add these databases to a pool** displays instructions for the Copilot skill only. No app-driven pool build, scaling, or scripted storage growth. |
| **3 · Verify Pool** (`/verify`) | Verify reality | Portal checklist for actual pool membership, followed by a separate application-identity SQL read check. |
| **4 · Search & Book** (`/appointments`) | Stay here | Compare Caldova-only and network searches. Read a native cross-database join view, then book with one conditional update to the owning database. |

## Measured serverless chapter

### Presentation focus: billed compute

The current presentation goal is a real Azure Monitor **bar chart** showing
billed compute near the 1-vCore online minimum, above it during activity,
back toward the minimum afterward, no compute charge during confirmed auto-pause,
and billing returning after resume. CPU-used/memory comparisons are diagnostic
detail, not a required portal presenter beat. The app still targets approximately
four CPU-used vCores and a ramp back down. Real achievement remains unproven;
the explicit scripted fallback now peaks at 4.00 and has passed a complete
Edge playback test (rise, fall, pause, resume, reset, return to measured view).
Historical experiment results below remain unchanged.

For average billed vCores, transform `app_cpu_billed` **Sum** from vCore-seconds
by dividing by the bucket duration in seconds in an Azure Monitor workbook.
Raw Metrics supports Sum, not Average, for this metric. With one-minute
granularity, 60 vCore-seconds is equivalent to 1 average billed vCore.
Use bars and accurate units; do not relabel raw Sum as vCores.

The rise/fall tests below do not constitute a single full pause/resume recording.
Close all SQL clients/workload connections and wait for actual auto-pause after
the configured 60-minute idle delay. Use ARM status/activity evidence to identify
the paused interval, not missing metric samples alone. A paused interval has no
compute charge, but missing samples must not be silently converted into zeros.
Record/time-lapse the real timeline as needed; separately approve the connection
that resumes the database. No live changes are implied by this presentation plan.

Configuration is under `ServerlessMonitor` in [appsettings.json](app/appsettings.json).
`Demo:Mode` must be `Measured`; there is no automatic scripted fallback.
The measured view has an explicit **Scripted illustration (fallback)** link to
`/serverless-illustration`, with persistent scripted labels and a return link.
It uses the original scripted workload only on that page; it never feeds
invented samples into the measured chart or runs SQL.

1. **Published 2026-10-23 (`/healthz` reports `sql-dmv`):** the app uses
   `sys.dm_db_resource_stats`, not Azure Monitor, for its CPU chart.
   ARM configuration is still read with `DefaultAzureCredential` to validate
   the standalone Hyperscale serverless target and display its configured range.
2. The observing identity needs Azure database-resource read access plus SQL
   access with `VIEW DATABASE STATE`. No permissions are granted automatically.
   Local diagnostic identity success does not prove the App Service identity
   has these permissions.
3. Click **Observe SQL CPU** only after approval for compute usage. Observation
   connects through `ConnectionStrings:Scheduling`, validates the exact
   primary database target, disables pooling/retries, and closes each poll's
   connection. Polling can resume compute and prevents auto-pause.
4. Polling reads primary-replica samples approximately every 15 seconds.
   The SQL DMV retains approximately an hour; the observation window is limited
   to 55 minutes. UTC timestamps are **interval ends**, not minute starts.
   Actual zeros remain zero; missing intervals are not generated or zero-filled,
   and chart lines break across gaps. Errors stop observation and workload.
5. **CPU used (average vCores)** =
   `avg_cpu_percent * cpu_limit / 100`, using each SQL sample's own limit.
   The raw percentage, limit, timestamp, and calculated value appear in the
   table. With a limit of 8, 50% is 4 average CPU-used vCores. This does not
   measure allocated cores or billed compute.
6. Billing stays in the portal. Show bars without hovering. `app_cpu_billed`
   Sum is vCore-seconds; divide by bucket seconds only when a workbook needs
   average billed vCores. No Azure Monitor CPU data feeds the app chart.
7. Before generating load, approve the target and cost, and restrict access to
   the demo app. Set `ServerlessMonitor__EnableWorkload=true` only in that
   approved environment. It is false by default; deployment does not enable it.
   Observation also executes SQL, so restrict access before exposing a
   managed-identity-enabled instance.
8. Click **Start SQL workload** with 1-16 workers for 1-10 minutes. The current
   synthetic CPU workload repeatedly hashes a scalar `varbinary(32)` value
   50,000 times per batch using `SHA2_256`. It accesses no tables, uses no joins
   or sorts, and requests no deliberate table-sized working set. It does not
   fabricate bookings, write data, or guarantee a particular CPU/billing curve. The SQL
   connection must match the monitored resource. Real load can resume the
   database and incurs charges. A worker error stops all workers with no retry.
9. Stop load and continue observing actual CPU behavior. Workers close
   their connections on stop; SQL observation continues until explicitly stopped. Leaving the
   page/disposal stops its circuit-scoped workload, and the duration limit
   bounds work even if a disconnected circuit is retained temporarily.
10. Stop **observation** and all other SQL clients before waiting for auto-pause.
    CPU-used zeros do not imply zero billed compute while online. A tiny scalar
    working set does not eliminate engine/session memory or guarantee CPU-only
    billing. Do not flush caches or change configuration to force chart values.

Record/time-lapse if needed, retaining UTC timestamps and labeling the recording
as time-lapsed. No sped-up app clock, extrapolated points, snapped vCore values,
or assumed pause. First prove workload intensity, visible rise/fall, and actual
metric delivery delay against the target; this source implementation is not
evidence that all live checks have passed. See the bounded live result below.

### Live result - 2026-10-22

#### SQL DMV diagnostic revision (latest)

The approved unchanged 16-worker scalar run on October 22 used SQL DMVs rather
than Azure Monitor for CPU measurement. It was bounded to three minutes but
stopped early after a SQL command timeout (`-2`): **257 completed batches,
one reported failure, no retry**. This is not a completed three-minute test.
All workload and diagnostic connections were disposed.

Read-only capture of retained primary-replica resource statistics confirmed:

| UTC interval end | CPU % | SQL vCore limit | Average CPU-used vCores |
|---|---:|---:|---:|
| 18:19:27.020 | 0.00 | 8 | 0.0000 |
| 18:19:42.060 | 2.44 | 8 | 0.1952 |
| 18:19:57.087 | 39.40 | 8 | 3.1520 |
| 18:20:12.113 | 6.33 | 8 | 0.5064 |
| 18:20:27.153 | 0.00 | 8 | 0.0000 |

The DMV path proves a real CPU rise and decline, **not approximately 4 vCores
or a successful full workload run**. One live session snapshot saw all 16
workload sessions, with a preceding session CPU delta averaging 0.9678 vCores;
it does not cover the later peak interval. Most sessions were sleeping in that
snapshot. The timeout's cause is not established. The failure exited before
the original harness printed its wait summary; no workload wait-delta
conclusion can be drawn from this run.

The harness now retains cooldown and wait summaries even when a workload
reports a failure, then exits nonzero. No second load run was performed.
The read-only `--serverless-dmv-observe-from <UTC timestamp>` path retrieves
retained SQL samples without starting workers; it still connects to SQL and
requires compute-use approval. `--serverless-live --serverless-workers 16`
uses a 30-second SQL baseline, at most three minutes of load, and a 60-second
SQL cooldown. ARM is used for target validation only, not Azure Monitor metrics.
Historical `--serverless-observe-from` remains management-only.

#### 16-worker repeat from an Azure client (2026-10-22 evening)

The unchanged 16-worker run was repeated from a West US 3 Windows 365 Cloud PC
(the database is in East US 2). It **completed the full three minutes** with
4,236 batches, 0 failures, and no timeouts. DMV CPU-used vCores rose from 0 to
5.86 in the first interval, held **7.97-7.99 (99.6-99.9% of the 8-vCore limit)**
for ten consecutive intervals, then returned to 0.00 in the first cooldown
interval. The top database wait delta was `SOS_SCHEDULER_YIELD` (1,381 s, nearly
all signal wait); `ASYNC_NETWORK_IO` was 4.4 s. This shows CPU saturation at the
configured maximum, not the earlier timeout's cause. The all-at-once start/stop
produced a step, not a gradual ramp.

#### Staged ramp (harness measured; app page not yet run live)

`--serverless-live --serverless-ramp` starts one worker per step, holds at the
peak, then stops one worker per step (last started first; each finishes its
in-flight batch, no command is cancelled). Defaults: peak 8 workers
(`--serverless-workers`), `--serverless-ramp-step-seconds 30`,
`--serverless-ramp-hold-seconds 60`, giving 0 -> 8 -> 0 over 8 minutes
(total limited to 10 minutes). The harness logs active workers alongside DMV
samples. The Patient Access page offers the same ramp, enabled by default.

The approved harness ramp from the same Cloud PC (SQL start 00:45:40, stop
00:53:40 UTC on 2026-10-23) completed **7,647 batches with 0 failures**. DMV
CPU-used vCores rose almost monotonically from 0.38 to 5.89 during the ramp
up, held 6.17, 6.09, and **6.94 (peak)** at 8 workers, fell from 5.99 to 0.81
during the ramp down, and reached 0.00 in the second cooldown interval. That is
about 0.75-0.87 vCores per worker; `SOS_SCHEDULER_YIELD` fell to about 200 s
(versus 1,381 s for 16 workers). Bob accepted the 6-7 vCore peak, so 8 workers
remains the default. This is one run from one client; the app-page ramp and
App Service publication are separate unverified gates.

#### Unified Patient Access page, pause, and resume (source; local only)

Patient Access is one page with **Scripted** (`/serverless-illustration`) and
**Measured** (`/`) modes and a single **Run** button. Both modes render the same
staff avatars, KPI tiles, live schedule, and vCore panel. In Measured mode,
staff online equals the actual number of active SQL workers, schedule entries
are labeled illustrative and advance only while SQL batches complete, and the
readout/gauge/chart use only SQL DMV samples.

Outside a run, Measured mode reads the database's Azure management status every
10 seconds (no SQL connection, so it does not prevent auto-pause) and shows
**Paused** or **Resuming**. **Sign in (resume)** opens one read-only connection
(`SELECT 1`) and automatically retries documented transient errors such as
40613 with capped backoff for up to three minutes, then reports the elapsed
time. Hyperscale's minimum auto-pause delay is **60 minutes**; close all SQL
clients (app runs, VS Code MSSQL, SSMS, portal query editor) at least about
65 minutes before the pause beat. This page has not yet completed a live
resume, and is not published.

On 2026-10-23 a measured app run (15 s up / 60 s hold / 45 s down) started at
about 16:23:40 UTC and closed its last SQL connection at about 16:33:25 UTC.
With no other clients, the Activity Log recorded a successful auto-pause at
**17:34:34 UTC** (about 61 minutes later), and the measured page displayed
**Paused** from Azure management status without opening a SQL connection.
**Sign in (resume)** then woke it: the Activity Log recorded a successful
resume at **18:44:05 UTC**, and per-minute `app_cpu_billed` returned to about
60 vCore-seconds (the 1-vCore minimum). The portal chart shows billing before
the pause, no billed values from 17:36 through 18:44, and billing again
from 18:45 (56.517 vCore-seconds, then about 60 per minute). The 17:35 bucket held 14.069 vCore-seconds as the pause completed.
Azure Monitor reports no value for paused minutes, not zero.

The app's measured chart has been changed in source to this DMV measurement.
All offline checks pass (**96** after the staged-ramp change). A read-only live check of the actual app
telemetry service returned **30 primary-replica samples**, including the
3.1520-vCore peak, using the diagnostic identity. No second load run was used.
On 2026-10-23 the DMV/ramp revision was published with `02-publish-app.ps1`;
`03-verify.ps1` passed and `/healthz` reports `"serverless":"sql-dmv"`.
App settings were unchanged (`ServerlessMonitor__EnableWorkload=false`, public
access). The scripted fallback is unchanged. App Service SQL permission and
readiness remain a separate, unverified gate.

#### Confirmed historical pause/resume

Management-only inspection found successful Activity Log records for:

- Auto-pause: **2026-10-21 20:54:22 UTC**.
- Auto-resume: **2026-10-22 16:01:41 UTC**.

Actual `app_cpu_billed` Sum at 15-minute granularity:

| UTC bucket start | Billed vCore-seconds |
|---|---:|
| October 21 20:00 | 905.52 |
| October 21 20:15 | 889.37 |
| October 21 20:30 | 905.49 |
| October 21 20:45 (partial before pause) | 599.97 |
| October 21 21:00 through October 22 15:45 | No samples |
| October 22 16:00 (includes resume) | 759.40 |
| October 22 16:15 | 1069.67 |
| October 22 16:30 | 1572.23 |

Values are illustrative rounded summaries of actual returned buckets; the paused
interval is confirmed by Activity Log, not inferred solely from missing samples.
Use **October 21 19:45 through October 22 16:45 UTC** for a historical bar
overview and zoom to the later measured rise/fall window below. Show the bars,
not hover tooltips. These management results do not claim that portal chart
rendering or a saved workbook has been visually rehearsed.

No new SQL connection, cache flush, compute change, or resume was used to collect
this historical evidence.

Approved test: same standalone 1-8 vCore database, four workers, three minutes,
read-only, no retries or Azure configuration changes. The stronger query
completed **1,659 reads with zero reported SQL failures**, from
**16:20:37.898 to 16:23:38.065 UTC**. The following actual Azure Monitor buckets
were captured afterward (one-minute granularity, maximum 8 vCores):

| UTC minute start | CPU % Average | CPU used, vCores | Billed Sum, vCore-seconds | Billed average vCores |
|---|---:|---:|---:|---:|
| 16:20 | 0.03125 | 0.0025 | 59.467 | 0.99112 |
| 16:22 | 13.21875 | 1.0575 | 127.209 | 2.12015 |
| 16:23 | 13.15625 | 1.0525 | 127.828 | 2.13047 |
| 16:25 | 0.07142857 | 0.00571429 | 59.466 | 0.99110 |

This proves a visible measured usage/billing rise and fall, not allocated-core
telemetry. It does **not** meet the ideal of CPU used exceeding 2 vCores for two
complete minutes. The app UI, deployed managed identity, and portal/workbook
visualization still need end-to-end rehearsal. Nothing was published.
Most-recent buckets were missing or revised as data arrived: the 16:25 billed
Sum increased from 45.347 to 59.466 on a later read. Do not treat the newest
clock-complete bucket as final or interpret missing values as zero.

#### Follow-up: 16 workers, same query

Approved follow-up changed only concurrency from four to 16, keeping the same
query, standalone 1-8 vCore configuration, and three-minute load limit.
SQL ran **16:35:38.336-16:38:38.788 UTC**, completing **2,990 reads with zero
reported SQL failures**. Workers stopped before the management-only cooldown.
Later captures returned:

| UTC minute start | CPU % Average | CPU used, vCores | Billed Sum, vCore-seconds | Billed average vCores |
|---|---:|---:|---:|---:|
| 16:35 | 0.046875 | 0.00375 | 60.468 | 1.00780 |
| 16:37 | 31.390625 | 2.51125 | 298.709 | 4.97848 |
| 16:38 | 31.40625 | 2.51250 | 304.227 | 5.07045 |
| 16:39 | 21.078125 | 1.68625 | 226.508 | 3.77513 |
| 16:40 | 0.125 | 0.01000 | 60.497 | 1.00828 |
| 16:41 | 0.125 | 0.01000 | 60.458 | 1.00763 |

This meets the earlier >2 CPU-used vCore threshold in two consecutive buckets
and shows a decline, but **does not meet the newer approximately 4 CPU-used
vCore target** (about 50% of the 8-vCore maximum). Billed compute near 5 vCores
is not CPU used near 5 vCores. Do not claim linear scaling from worker count or
invent a cause for the remaining gap without diagnostics.

#### Small-working-set revision and validation

Before changing the join query, actual `app_memory_percent` for the
16:35-16:43 UTC test was retrieved: one-minute averages were approximately
**8.50-8.81%**, with maxima approximately **9.42-9.56%**. These percentages
do not establish why billed compute exceeded average CPU used. That gap must
not be described as proven memory-driven billing.

The current source replaces the join query with the bounded scalar hash batch
described above and captures memory beside CPU and billing. An approved
16-worker, three-minute test started at **2026-10-22 17:08:40.705 UTC**.
It completed **38 batches**, then reported SQL login transport error **10053**:
"An established connection was aborted by the software in your host machine."
The harness canceled workers, disposed their connections, and exited without
retry. The error occurred during login to the redirected SQL endpoint, not as
a reported query memory error. Its root cause is unproven.

That first attempt was **not** a completed scaling test.
An explicitly approved unchanged repeat ran **17:18:52.393-17:21:53.964 UTC**,
completing **1,891 batches with zero reported SQL failures**. A later
management-only refresh captured:

| UTC minute start | CPU used, vCores | Billed average vCores | App memory %, Average |
|---|---:|---:|---:|
| 17:18 | 0.00500 | 1.00848 | 8.50537 |
| 17:20 | 0.58500 | 1.38783 | 8.49414 |
| 17:21 | 0.81750 | 1.63983 | 8.49658 |
| 17:22 | 0.79000 | 1.67095 | 8.49561 |
| 17:23 | 0.00625 | 1.00977 | 8.50000 |
| 17:24 | 0.00500 | 1.00960 | 8.52393 |

The scalar query executes successfully with nearly flat app memory percentage,
but **does not meet the four CPU-used vCore target** and produces less measured
CPU load than the previous join workload. Different achieved CPU demand means
this is not proof of memory-related cost savings. The billing/CPU gap remains
unexplained by these observations. Do not promise CPU-only billing or repeat
load tests without an evidence-driven diagnostic hypothesis.
The previous join-workload window remains the stronger presentation evidence.
The MSSQL extension currently exposes no connection profile for this Caldova
server, so query-plan/resource-governance diagnostics have not been executed
through that diagnostic path. No cache flush, firewall, capacity, role, or
retry-policy change was made. All load workers stopped and connections were
disposed; subsequent observation used management APIs only. Source changes
were not published.

The preferred portal rehearsal window remains **2026-10-22 16:35-16:43 UTC**
(11:35-11:43 CDT); the earlier window above remains valid historical evidence.
On the actual Caldova database, use **Monitoring > Metrics**:

- Chart 1: **App CPU percentage**, **Average**, one-minute granularity.
- Chart 2: **App CPU billed**, **Sum**, same time range and granularity.
- Diagnostic chart: **App memory percentage**, **Average**, same window.
- Keep raw units explicit, or transform both into average vCores in a workbook.
  Do not overlay percent and vCore-seconds as equivalent units.

If using the scripted illustration, still show these actual portal charts.
State that their historical window is independent of the illustration's clock;
the illustration does not generate or prove the charges. Choosing fallback is
an explicit presenter action, never an error-triggered success substitute.

The existing approved live-check harness is
[ServerlessLiveCheck.cs](tests/ServerlessLiveCheck.cs).
`--serverless-live` incurs real compute usage and requires new approval for
each run. It defaults to four workers; use `--serverless-workers 16` only when
that concurrency is explicitly approved (accepted range 1-16; duration remains
three minutes). `--serverless-ramp` replaces the fixed run with the staged
0 -> peak -> 0 schedule described above and also requires per-run approval.
`--serverless-observe-from <UTC-minute>` performs management reads
only; use it only for a window whose configured maximum was already recorded.
No automatic booking test is part of either mode.
`--serverless-dmv-observe-from <UTC timestamp>` instead performs SQL reads of
retained primary-replica intervals and verifies the app telemetry service.
It starts no workers but requires approval for SQL compute use; it is not a
management-only or auto-pause-safe observation mode.

Official references:
[serverless monitoring](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-monitor),
[SQL resource statistics](https://learn.microsoft.com/sql/relational-databases/system-dynamic-management-objects/sys-dm-db-resource-stats-azure-sql-database?view=azuresqldb-current),
[supported metrics](https://learn.microsoft.com/azure/azure-monitor/reference/supported-metrics/microsoft-sql-servers-databases-metrics),
[metrics REST API](https://learn.microsoft.com/rest/api/monitor/metrics/list).

## Platform facts

Verified against Microsoft Learn on 2026-10-15:

- Hyperscale serverless `HS_S_Gen5_8` scales between **1 and 8 vCores**; max data size **128 TB**
  ([resource limits](https://learn.microsoft.com/azure/azure-sql/database/resource-limits-vcore-single-databases)).
- CPU consumption is measured rather than inferred from signed-in staff.
  The app does not infer runtime scaling increments or snap measurements.
  Usage is metered per second
  ([billing](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-billing)).
- Billed compute includes CPU, memory, and a configured minimum. Do not infer
  its fall time from CPU alone; show the measured series.
- Hyperscale serverless **auto-pause is in preview**; the default and **minimum delay is 60 minutes**;
  named replicas aren't supported with auto-pause
  ([auto-pause](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-auto-pause-resume)).
  The live demo can't pause on camera — show a pre-recorded pause later.
- **Auto-resume**: the first connection to a paused database starts the resume; that attempt fails
  with error 40613 and the app must retry. Databases generally resume in less than one minute
  ([auto-resume troubleshooting](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-auto-pause-resume#auto-pause-troubleshooting)).
- **Serverless is single-database only**; elastic pools use provisioned compute. Moving Caldova into
  the pool *is* the upgrade from serverless to provisioned
  ([serverless overview](https://learn.microsoft.com/azure/azure-sql/database/serverless-tier-overview)).
- Moving a database into a Hyperscale elastic pool requires matching zone-redundancy and backup
  storage redundancy settings
  ([Hyperscale elastic pools](https://learn.microsoft.com/azure/azure-sql/database/hyperscale-elastic-pool-overview)).
- Native cross-database private preview (onboarding supplied 2026-10-21; guide saved in
  [docs/crossdb-private-preview.md](docs/crossdb-private-preview.md)): three-part
  names across online databases on the same logical server and supported elastic pool.
  Microsoft Entra authentication requires a server login and users mapped from that
  login in each database, with object permissions in each. Reads can span databases;
  each transaction can write to only one database. Savepoints are not supported.
  This replaces the earlier elastic-query/external-table plan. The app uses this
  path after the SQL setup below is deployed.

## Application data paths

The UI keeps the serverless chapter separate from the SQL appointment workflow:

| Interface | Implementation | Data source |
|---|---|---|
| `ServerlessMonitor` / `ServerlessSqlWorkload` | Azure Monitor REST / bounded SqlClient workers | Actual one-minute CPU/billing observations and real read workload; no scripted default |
| `IAppointmentAvailabilityService` | `SqlAppointmentAvailabilityService` | One source connection to `caldova-scheduling`; join-view reads and provider-specific conditional updates |

`network.AppointmentAvailability` joins Caldova's central `network.ProviderClinic`
directory to each provider's `dbo.AppointmentSlot` table by clinic ID and provider.
`UNION ALL` combines those joined schedules. Only network members and unbooked
slots are exposed; searches filter to future UTC slots. The UI displays the same
SQL view file embedded into the assembly, not a separately maintained example.

Booking uses a fixed provider-to-database allowlist, parameterized slot/key values,
and `UPDATE ... WHERE IsBooked = 0 ... OUTPUT inserted.SlotId`. It checks current
network membership, writes only the owning provider's database, and uses no
explicit transaction or automatic retry. Concurrent attempts cannot both book
the same slot. A lost response is reported as unconfirmed, never success.

## SQL setup

For scripted deployment using the current Azure CLI user's credentials, use
`deploy\07-deploy-scheduling.ps1 -Phase Schema`, then move the databases into
the pool, then `-Phase Network` and `-Phase Verify`. This requires PowerShell's
`SqlServer` module and obtains a fresh SQL access token in memory. The Verify
phase checks the deployment identity, not the app identity.
`deploy\08-configure-app-sql.ps1` configures the app's managed-identity connection
and exact current app-egress firewall rules. The separately requested **Allow
Azure services** setting is enabled in Azure; this script does not remove it.

Run through the MSSQL extension as the Entra administrator, on separate
connections to the specified databases (no `USE` statements):

1. `sql\00-app-login.sql` in `master`: create the app managed identity's server login.
2. `sql\01-scheduling-schema.sql` separately in all three scheduling databases:
   create local appointment tables and seed synthetic future UTC slots; Caldova
   also gets the central provider/clinic directory.
3. Add all three databases to the existing enabled pool using the skill below.
4. `sql\02-network-view.sql` in Caldova: create the native cross-database join view.
5. `sql\03-app-permissions.sql` separately in all three: users **from the login**,
   `SELECT`/`UPDATE` on appointment tables, plus directory/view reads in Caldova.
   Existing mismatched users cause an explicit error; never silently replace them.
6. Verify `sql\04-verify-read.sql` from Caldova using the application identity.
   Then use app steps 3 and 4 to verify actual reads and booking.

Scripts use the current fixed database names and app identity
`app-caldova-network-0efc44`. If porting, coordinate SQL files, the provider
allowlist, source-catalog guard, connection settings, and portal instructions.
Duplicate Entra display names require the documented Object ID login syntax;
use the managed identity's object ID, not a client ID. No SQL login is needed.

The synthetic schema has no triggers or additional audit writes. Do not add a
write to another database to the booking statement/transaction. No app DDL,
`db_owner`, broad server roles, or directory writes are granted.

Seed reruns insert missing dates without deleting data or reopening bookings.
Run setup sequentially; it is not a concurrent migration runner. The initial
Cardiology seed is Caldova +19 days, Ridgeview +2 days; actual search results
depend on today's data and bookings.

Local configuration uses `ConnectionStrings:Scheduling` with
`Authentication=Active Directory Default`. The local developer also needs their
own Entra server login, login-mapped users, and equivalent permissions; the app
identity grants do not grant the developer access. For Azure, `01-deploy-app-service.ps1`
sets a passwordless `ConnectionStrings__Scheduling` override using
`Authentication=Active Directory Managed Identity`. The existing App Service
must have network access to SQL (presenter firewall access does not prove app
access). `/healthz` checks application liveness only, never SQL readiness.

Azure layout for measured mode (all in `caldova-network-rg`, deployed by `04-deploy-sql.ps1`):

- Logical server `sql-caldova-network-0efc44`, East US 2, **public endpoint + firewall**.
  Microsoft Entra-only authentication is retained for the native cross-database preview.
- `caldova-scheduling` — Hyperscale serverless `HS_S_Gen5_8`, min 1 vCore, auto-pause 60 min.
- `northbrook-scheduling` (provisioned `HS_Gen5_2`), `ridgeview-scheduling` (provisioned
  `HS_Gen5_4`).
- `caldova-network-pool` — Hyperscale elastic pool, 8 vCores, created empty.
- Part 1 server `sql-caldova-start-0efc44` with its own serverless `caldova-scheduling`
  (deployed by `09-deploy-part1-sql.ps1`; see [Separate Part 1 serverless database](#separate-part-1-serverless-database)).
- Every database and the pool use **Local** backup storage redundancy and **no** zone redundancy,
  so the databases can move into the pool later.
- App will connect passwordless with its system-assigned managed identity, using an
  Entra server login and login-mapped users. No elastic-query SQL login or password
  is needed.

`Demo:Mode` accepts only `Measured`. It does not switch appointments to scripted data. SQL failures are
shown in the app and logged server-side.

## Backlog

Remaining work and presentation options.

| # | Item | Notes |
|---|---|---|
| 1 | **Rehearse Copilot-assisted sizing/deployment** | The Start and Grow skill defines requirements, recommendation, change summary, and approval. Verify supported sizing and exact deployment scope; the current SQL provisioner creates all databases and a pool, not just Caldova. |
| 2 | **Enable auto-pause live in the Azure portal** | Show auto-pause being turned on in the portal instead of setting it at create time. `04-deploy-sql.ps1` currently creates `caldova-scheduling` with `--auto-pause-delay 60`; change it to create with auto-pause off when this lands. Hyperscale auto-pause is preview with a 60-minute minimum delay. |
| 3 | **Live serverless verification** | Measured source path implemented; approve monitoring permissions/load and prove actual rise/fall, sample delay, and agreement with Azure Monitor before publishing/presenting. |
| 4 | **Live native cross-database verification** | **Blocked (2026-10-24).** Schema is deployed in all three databases (Ridgeview was missing it). With all three Online in `caldova-network-pool`, three-part names fail with Msg 40515 and `SUSER_SID('<name>')` fails with Msg 40507 in user databases, so the preview is not active for this server/pool. Ask the preview team to enable or re-check `sql-caldova-network-0efc44` / `caldova-network-pool`, then run `07-deploy-scheduling.ps1 -Phase Network` and `-Phase Verify` while pooled. Databases were returned to standalone. |
| 5 | **Independent Start/Grow chapters** | Part 1 now ends independently; chapter entry/navigation grouping remains to implement, preserving routes and telemetry/SQL disclosures. |
| 6 | **Pool scaling under workload** | Implement an approved capacity change on the existing pool, bounded workload, and measured transition/workload behavior. Connection retry handling is a talking point only; no new retry logic, instrumentation, or tests. Preserve existing booking behavior. |
| 7 | **Measured database growth** | Add bounded approved synthetic inserts and measured data-space-used growth within the workload flow, with fixed compute and scoped cleanup. No separate storage/I/O checkpoint page. Not yet implemented. |

## Move into the pool / reset to standalone

Use the workspace skill **start-and-grow-hyperscale-demo** (its "Pool membership"
section) in the same Copilot chat used for the rest of the demo, with prompts such as:

- "Integrate Caldova's acquired providers into the scheduling pool."
- "Return the providers to standalone and restore Caldova serverless."

The skill previews the change, obtains approval, then runs the canonical script.
The same script can be run directly:

```powershell
.\deploy\06-set-pool-membership.ps1 -Mode Pooled -WhatIf
.\deploy\06-set-pool-membership.ps1 -Mode Pooled
.\deploy\06-set-pool-membership.ps1 -Mode Standalone -WhatIf
.\deploy\06-set-pool-membership.ps1 -Mode Standalone
```

Stop appointment/workload traffic first. Changes run synchronously and sequentially;
connections may be interrupted. Every database is checked before the first update,
and every move is verified afterward. A failed operation stops without automatically
undoing earlier moves; inspect `05-verify-sql.ps1`, then rerun the desired direction.
`-WhatIf` performs read-only Azure preflight and never updates databases.

Pooled mode converts Caldova to provisioned compute and adds all three databases to
the **existing** Hyperscale pool. Standalone mode restores Caldova to serverless
`HS_S_Gen5_8` (1-8 vCores, 60-minute auto-pause), Northbrook to `HS_Gen5_2`, and
Ridgeview to `HS_Gen5_4`. The empty pool is retained so preview enablement is not
lost. Neither direction deletes data or changes identities, grants, or app state.
Auto-pause is configured, not immediate. Configuration overrides are the same as
the other deployment scripts.

These operations verify Azure placement/compute, not SQL cross-database access.
Native queries require all three databases online in the same pool, an Entra
server login and login-mapped users in every participating database, and object
permissions. Entra-only authentication remains enabled. Native cross-database
queries stop working when the databases return to standalone.

Regression tests (mocked Azure CLI; no live changes):

```powershell
.\deploy\tests\pool-membership.tests.ps1
```

Application query-contract and component-rendering checks (no SQL connections):

```powershell
dotnet run --project .\tests\CaldovaNetworkScheduling.Checks.csproj --configuration Release
```

After approved SQL setup, run the same command with `-- --live` to execute the
real view and race two bookings for one Ridgeview slot. It asserts exactly one
winner and that the slot disappears from availability. **This commits one
synthetic booking and leaves it booked.** It uses `ConnectionStrings__Scheduling`
from the environment and requires the intended test identity's permissions.
Do not run it during pool moves or without approval for the write.

## Run locally

```powershell
cd app
dotnet run --launch-profile http    # http://localhost:5191, Part 2 server
dotnet run --launch-profile part1   # http://localhost:5099, Part 1 server, measured load enabled
```

### Separate Part 1 serverless database

Part 1 needs Caldova serverless and able to auto-pause; Part 2 needs Caldova in the
pool. To keep them from interfering, Part 1 runs against its own logical server,
`sql-caldova-start-<suffix>`, with a database of the same name, `caldova-scheduling`
(serverless 1-8 vCores, 60-minute auto-pause, Entra-only). The `part1` launch profile
overrides `ConnectionStrings__Scheduling` and `ServerlessMonitor__ResourceId`; no code
changes. The Part 1 workload and telemetry use only DMVs, so this database has no
schema. Steps 2-4 of the app are not used with this profile.

```powershell
.\deploy\09-deploy-part1-sql.ps1 -WhatIf                                  # plan only
.\deploy\09-deploy-part1-sql.ps1 -ClientIp <presenter-ip> [-AllowAzureServices]
```

This is the on-camera deployment in Beat 1.2 (the skill runs it after approval).
`-MinVCores`, `-MaxVCores`, and `-AutoPauseDelay` override the config defaults
(1, 8, 60). The script is idempotent and verifies the result. Use
`-AllowAzureServices` when presenting from an
Azure-hosted machine whose outbound IP changes per connection. It sets the
`PresenterClient` firewall rule to `-ClientIp` (or this machine's current IP).

## Deploy to Azure App Service

Requires PowerShell 7, Azure CLI signed in, and the .NET 10 SDK. Subscription comes from
`AZURE_SUBSCRIPTION_ID` or the current `az` account.

```powershell
./deploy/01-deploy-app-service.ps1   # resource group, Windows S1 plan, web app (.NET 10, WebSockets, ARR affinity)
./deploy/02-publish-app.ps1          # dotnet publish + zip deploy
./deploy/03-verify.ps1 -Open         # endpoint checks, then open the app in an Edge app window
./deploy/04-deploy-sql.ps1           # logical server, serverless + provisioned Hyperscale DBs, empty Hyperscale pool
./deploy/05-verify-sql.ps1           # show server, region, databases, and pool state
```

Optional overrides: `CALDOVA_NETWORK_RESOURCE_GROUP`, `CALDOVA_NETWORK_APP_LOCATION`,
`CALDOVA_NETWORK_APP_PLAN`, `CALDOVA_NETWORK_APP_NAME`, `CALDOVA_NETWORK_SQL_SERVER`,
`CALDOVA_NETWORK_SQL_LOCATION`.

Step 1 observation history is held in memory per browser circuit. Pool state lives in Azure; appointments
and bookings live in SQL and survive app restarts. Next/Back navigation does not
mark pool membership as complete.

## Remove

```powershell
./deploy/99-remove.ps1   # deletes caldova-network-rg after confirmation
```
