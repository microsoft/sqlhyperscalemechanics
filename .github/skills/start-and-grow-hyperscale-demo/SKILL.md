---
name: start-and-grow-hyperscale-demo
description: 'Build, repair, verify, and present the two-part Caldova Start and Grow Hyperscale demo in startandgrowhyperscale/ of the sqlhyperscalemechanics repository. Use when Bob says "build the start and grow demo", "update startandgrowhyperscale", "prepare the Mechanics Hyperscale demo", "run the start and grow demo", "rehearse start and grow", "start the serverless chapter", "start the grow chapter", "integrate Caldova''s acquired providers into the scheduling pool", "integrate the Caldova acquisitions", "add the Caldova databases to the pool", "remove the Caldova databases from the pool", "return the providers to standalone", "restore Caldova serverless", or asks for the next beat while this demo is active. This is the single skill for every on-camera GitHub Copilot interaction in this demo (one chat session). Covers Copilot-assisted sizing/deployment, serverless Patient Access, acquisitions and pool membership (Pooled/Standalone), pool scaling under workload with retry handling as a talking point only, independent storage, and native cross-database queries. Does not run unrelated Caldova, Ward General, indexing, or HorizonDB demos. Build and live Azure changes are separate from presentation cues and require explicit approval.'
---

# Start and Grow Hyperscale

Help Bob build the agreed demo correctly, prove each capability, and present it
one beat at a time. There are two independently presentable chapters:

1. **Start:** Portal choices -> Copilot-assisted sizing/deployment -> serverless
   range and Caldova Patient Access -> billing evidence.
2. **Grow:** acquisitions -> serverless to elastic pool -> pool scaling under
   workload and measured database growth -> cross-database queries.

## Canonical sources

All command paths below are relative to the `sqlhyperscalemechanics` repository root.

- [Runbook](../../../startandgrowhyperscale/DEMO-RUNBOOK.md):
  locked chapter/beat order, presenter cues, evidence gates, and reset.
- [Recording script](../../../startandgrowhyperscale/RECORDING-SCRIPT.md):
  take-by-take recording order and current readiness.
- [README](../../../startandgrowhyperscale/README.md):
  current implementation, setup, commands, validation, and measured evidence.
- [Configuration](../../../startandgrowhyperscale/deploy/00-config.ps1):
  resource names, subscription selection, compute, and redundancy.
- [Pool membership script](../../../startandgrowhyperscale/deploy/06-set-pool-membership.ps1):
  canonical Pooled/Standalone implementation; see "Pool membership" below.

Read the runbook and README first. Read configuration and relevant implementation
before issuing commands or changing code. Derive actual resource targets from
configuration/environment and live inspection, not historical README evidence.
Never reuse a past approval for a new operation.
Source, published package, ARM configuration, SQL readiness, and measured
behavior are distinct; report each separately.

## Select a mode

- **Build/update/repair:** implement the requested capability in the existing kit.
  A request to create or update this skill is not permission to deploy resources.
- **Verify/preflight:** inspect and test readiness without changing Azure
  configuration. SQL reads can resume serverless compute and incur charges;
  disclose this before connecting to a paused database.
- **Present/rehearse:** cue the selected chapter one beat at a time. Do not
  deploy, publish, move databases, or scale compute merely because Bob says
  "next". If a cue requires an Azure change, obtain approval for that change.
- **Reset:** distinguish clearing local observations, restoring standalone compute,
  restoring an approved pool capacity, and changing synthetic bookings.
  Never infer deletion, reseeding, or restoring bookings from "reset".

For ambiguous requests, clarify the mode and chapter before acting. If Bob says
"run the demo" without selecting a chapter, preflight and begin Part 1.
At the end of Part 1, stop; do not automatically start Part 2.

## Build workflow

1. Inspect the current source and README evidence against the runbook's readiness
   table. Show a short gap list: implemented, locally verified, live verified,
   or blocked. Do not claim the two-chapter UI or pool-scaling beat already exists.
2. Establish the known-good baseline. Preserve historical Azure Monitor evidence,
   use SQL DMV samples for the app's CPU measurement,
   native SQL paths, permissions, and pool membership helper. Work on one
   bounded capability at a time, in chapter order unless Bob selects another.
3. Build the Part 1 deployment interaction:
   - Gather workload/concurrency, data size/growth, activity/idle pattern,
     latency/warm-up tolerance, region, availability/redundancy, network/auth,
     and budget requirements. Ask only for information not already supplied.
   - Recommend a supported starting configuration, including serverless min/max
     and auto-pause suitability. The existing 1-8 vCore range is the demo
     baseline, not a sizing formula. Do not invent price or capacity estimates.
   - Verify current options, limits, and billing against Microsoft Learn.
   - Present a change-management summary: inputs, assumptions, sizing rationale,
     exact target/resources, compute range, availability/network choices,
     estimated cost source/date if available, proposed actions, verification,
     and rollback limits. Missing inputs remain explicitly unresolved.
   - The skill/repository to show is this skill at
     `https://github.com/microsoft/sqlhyperscalemechanics/tree/main/.github/skills/start-and-grow-hyperscale-demo`.
     The repository is private; confirm the intended branch, that it is
     published, and that it is acceptable on camera before showing the link;
     do not imply uncommitted local changes are already on GitHub.
   - Before deployment, show the exact plan and obtain approval. Read each
     deployment script first: the current SQL provisioner creates all three
     databases plus an empty pool, not just the first Caldova database.
     Do not call that a single-database-only deployment or silently run the
     broader setup on stage. Add a scoped path if that is the approved goal.
4. Separate the app's chapter entry points and navigation without losing the
   existing routes or telemetry/SQL disclosures. Part 1 must end independently;
   Part 2 must be directly accessible after its own preflight.
5. Keep pool membership in `deploy\06-set-pool-membership.ps1` and the
   "Pool membership" section below. Stop dependent SQL
   traffic while databases move. Retain the existing preview-enabled pool.
6. Implement pool scaling as a separate approved operation on that same pool:
   - Verify supported source/target capacity, hardware, per-database limits,
     redundancy, quotas, and CLI/API syntax before adding commands.
   - Record before/after ARM configuration and operation status. Make targets
     explicit and require approval; do not invent a scale target in the skill.
   - Run a bounded, cancellable workload before, during, and after scaling.
     Unlike membership moves, keep this workload running through the scale
     transition on the fully pooled databases.
   - Measure timestamped successes, latency, SQL/connection errors, and
     observed workload recovery. Label ARM
     completion separately from application recovery.
   - Retry handling is a presenter talking point only: explain that applications
     need connection retry handling for a possible brief switchover interruption.
     Do not add retry logic, retry instrumentation, or retry tests for this demo.
     Do not claim the app demonstrates retries. Preserve existing booking
     behavior; never silently replay an uncertain booking write.
   - A run with no observed errors proves only that no interruption was
     observed in that run. Never manufacture a failover or promise zero downtime.
7. Show actual database growth within the workload flow, not a separate storage/
   I/O checkpoint page. Implement bounded synthetic inserts and measure actual
   data space used while compute stays fixed; approve the volume/cost and cleanup
   separately. This growth path is not yet implemented. Do not substitute fake
   growth controls, booking updates, or an unmeasured claim. Explain independent
   storage without implying unlimited I/O or wholly compute-independent limits.
8. Preserve cross-database search and the conditional single-provider booking.
   No external-table substitution, multi-database write transaction, additional
   audit write, broad roles, SQL passwords, or scripted appointment fallback.
9. Update the runbook and README readiness status only to the level proven.
   Validate locally before offering publication, then verify the deployed
   application's identity separately from the deployment identity.

## Build and verification commands

Run the smallest checks relevant to the implementation. These do not access SQL:

```powershell
dotnet build .\startandgrowhyperscale\app\CaldovaNetworkScheduling.csproj --configuration Release
dotnet run --project .\startandgrowhyperscale\tests\CaldovaNetworkScheduling.Checks.csproj --configuration Release
```

For pool membership changes, also run the mocked CLI checks:

```powershell
& .\startandgrowhyperscale\deploy\tests\pool-membership.tests.ps1
```

Use `deploy\05-verify-sql.ps1` for live ARM placement/configuration inspection
and `deploy\03-verify.ps1` for deployed page/liveness checks, with the full kit
path from the workspace root. Neither proves application SQL readiness.
Use the MSSQL extension for ad-hoc SQL, schema, and diagnostics. Canonical
`deploy\07-deploy-scheduling.ps1` Schema/Network/Verify phases are deployment
operations, not ad-hoc queries; obtain approval before running setup.
The Verify phase uses the deployment identity, not the app identity.

The application checks with `-- --live` commit one synthetic Ridgeview booking.
Require explicit approval and the intended identity/configuration before running;
never classify this as read-only preflight. Keep the existing concurrency and
unknown-write-outcome checks.

All PowerShell scripts run synchronously, one script per call; check completion
and exit code, and surface the full error before filtering. Do not start
background deployment jobs or issue `--no-wait`. If the tool returns a running
session, wait for completion and read that same session; do not rerun the script.

## Present one beat at a time

1. Read the runbook and preflight the chosen chapter. If a required capability
   is unbuilt or unverified, say so and stop that live beat. Offer build/repair
   separately; never substitute a scripted success.
2. Track mode, chapter, current beat, done/skipped beats, readiness blockers,
   and approved operation scope in conversation/session state. Do not write
   tracking files into the repository. Across sessions, re-establish state
   and approvals rather than guessing.
3. For each beat show only:
   - **Say:** the concise presenter line.
   - **Do:** exact page/click/prompt/approved command.
   - **Prove:** required observable result and its limitations.
4. Stop after each beat. Advance only on "next", "done", or an explicit beat
   selection. "Back" re-cues; "skip" records the skip. Do not dump the runbook.
5. **Current presenter goal:** an actual Azure Monitor billed-compute bar chart:
   online minimum -> above minimum with activity -> back toward minimum ->
   no compute charge during confirmed auto-pause -> billing returns on resume.
   Bob's 2026-10-23 app target is the staged ramp: 0 -> 8 -> 0 workers, one
   per 30 seconds, 60-second hold, 8 minutes; a peak of 6-7 CPU-used vCores
   is acceptable. Patient Access is one page with Scripted (`/serverless-illustration`)
   and Measured (`/`) modes sharing the staff/KPI/schedule scene and vCore panel;
   measured staff online equals active SQL workers and patient entries are
   labeled illustrative. Measured mode shows Paused/Resuming from Azure
   management status (no SQL) and has a **Sign in (resume)** button. Bob decided
   (2026-10-23) that this read-only first sign-in retries transient errors
   such as 40613 automatically so no error appears on stage; this exception
   covers only that read-only resume sign-in, not bookings or pool scaling.
   Hyperscale's minimum auto-pause delay is 60 minutes: close every SQL client
   at least ~65 minutes before the pause beat, or record with disclosure. The harness ramp measured this from an Azure client; the
   app-page ramp is published (2026-10-23) but unrun; load is disabled. The explicitly labeled
   illustration still peaks at 4.00 and its complete Edge cycle is verified.
   Do not make CPU/memory comparison or elimination of memory-driven billing
   a portal demo requirement. Show billing bars without hovering over numbers
   or narrating unit calculations; keep the underlying chart labels accurate.
   Transform `app_cpu_billed` Sum / bucket seconds into average billed vCores
   in a workbook; the raw metric supports Sum in vCore-seconds, not Average.
   For raw one-minute bars, the setup equivalence is 60 vCore-seconds per average vCore.
   Close all SQL clients before waiting for auto-pause. Use ARM status/activity
   evidence, not SQL polling or missing bars, to confirm pause. Never fabricate
   zero samples to fill a paused gap. Separately approve resume/compute usage.
   Historical Activity Log confirms pause on 2026-10-21 at 20:54:22 UTC and
   resume on 2026-10-22 at 16:01:41 UTC; billing is absent in the confirmed
   paused period and returns afterward. Use the runbook's long historical
   view plus the separate rise/fall view, or capture a new complete timeline.
   Do not imply these separate windows are a synchronized new recording.
   Recording and disclosed time-lapse are acceptable.
   Current app source uses `sys.dm_db_resource_stats` primary-replica samples,
   not Azure Monitor, for CPU. Calculate average CPU-used vCores as
   `avg_cpu_percent * cpu_limit / 100`, using each sample's own limit and UTC
   interval-end timestamp. Samples arrive approximately every 15 seconds.
   Observation opens SQL connections, can resume compute, and prevents
   auto-pause; stop observation and all clients before the pause beat.
   It requires SQL `VIEW DATABASE STATE`; never grant it automatically.
   The DMV/ramp revision was published 2026-10-23 with load disabled. A laptop 16-worker run
   reached 3.152 vCores then timed out after 257 batches (cause unknown).
   From the West US 3 Cloud PC, the unchanged 16-worker run completed but
   pinned the 8-vCore limit (a step, not a ramp). The approved 8-worker ramp
   on 2026-10-23 00:45-00:54 UTC completed 7,647 batches with no failures:
   0 -> 6.94 peak -> 0 vCores, about 0.75-0.87 vCores per worker. One run
   from one client; do not generalize it or run more load without approval.
   Actual billing remains in Azure Monitor: `app_cpu_billed` Sum / bucket
   seconds is average billed vCores. Never use the scripted scene as live proof.
   Show raw values, UTC timestamps, and gaps; never infer zero from missing data
   or label consumption as allocated cores. Verify agreement with the same
   portal range/granularity. Accept telemetry delay and record/time-lapse if
   needed, labeling the recording rather than accelerating the app clock.
   Real SQL load is opt-in, bounded, and requires target/cost approval plus
   restricted app access. Prove the actual billing timeline and sample delivery before
   calling this demo ready. Do not claim direct cache warm-up measurement.
   Historical Azure Monitor captures include `app_memory_percent` Average.
   Do not conflate it with SQL DMV memory percentages. Use the current
   bounded scalar CPU workload to avoid deliberate large data scans/grants;
   validate it before claiming memory-related savings. The 2026-10-22 scalar
   repeat completed with app memory near 8.5% but only 0.8175 peak CPU-used
   vCores, below the four-vCore goal; use the README evidence rather than
   inheriting the older join workload's stronger measured curve.
   Never flush caches to align the chart, promise CPU-only billing, or attribute
   the measured CPU/billed gap to memory without evidence.
   Bob explicitly permits a scripted illustration fallback if live timing is
   unsuitable. Stop measured work first, then explicitly choose
   `/serverless-illustration`; preserve all scripted labels and never inject
   its samples into measured series. Real CPU-used/billed portal charts remain
   optional diagnostics; the real billed-compute portal chart is mandatory,
   even when its disclosed historical window differs from the
   illustration. Use the runbook's measured window and evidence limitations.
   Never auto-switch to an illustration on an error or call it live proof.
6. For Part 2 use the "Pool membership" procedure's preflight/approval and postconditions.
   For scaling, describe service-prepared compute and final switchover with
   possible brief connection interruption; discuss the need for connection
   retry handling without implementing or demonstrating it. Show the actual
   workload observations, not a claimed retry demonstration.
7. When asked to run/open the app, verify reachability and open a standalone
   Microsoft Edge app window. `deploy\03-verify.ps1 -Open` supports the deployed
   app. For a local URL, probe both Program Files Edge locations and launch
   `msedge.exe --app=<URL>` with `Start-Process`; no embedded browser substitute.
8. Part 1 closes after billing. Part 2 closes after native search/booking.
   Neither completion automatically resets Azure or reopens booked slots.

## Pool membership

Caldova Health has acquired Northbrook Family Clinics and Ridgeview Specialty
Care. Bring their scheduling databases together in the existing Hyperscale
elastic pool, or return them to standalone configurations. On camera, run this
in the same Copilot chat used for Beat 1.2; do not switch to another skill.

- **Pooled**: all three databases join the existing `caldova-network-pool`.
  Caldova first leaves serverless for provisioned Hyperscale compute.
- **Standalone**: remove the three databases from that pool. Restore Caldova
  to `HS_S_Gen5_8`, min 1 vCore, auto-pause 60 minutes; Northbrook to
  `HS_Gen5_2`; Ridgeview to `HS_Gen5_4`. Values come from `deploy\00-config.ps1`.
  Keep the pool, databases, data, users, and objects.
- **Inspect only**: run `deploy\05-verify-sql.ps1`; do not mutate anything.

If the direction is unclear, ask in plain text. Never interpret "reset" as
permission to delete or recreate resources.

1. Preflight the selected direction with `-WhatIf`:

   ```powershell
   & .\startandgrowhyperscale\deploy\06-set-pool-membership.ps1 -Mode Pooled -WhatIf
   ```

   Use `-Mode Standalone -WhatIf` for removal. The pool must already exist and
   be enabled for the private preview; ARM membership does not prove preview
   enablement or SQL permissions.
2. State the subscription, server, pool, and three database names and current
   compute. Explain the compute change, possible connection interruptions, and
   that pooled Caldova disables Part 1's measured mode until Standalone is
   restored. Ask for plain-text approval and wait. Stop dependent app/workload
   traffic first; cross-database traffic must not run while databases are split
   across pool boundaries.
3. Apply exactly the approved direction without `-WhatIf`, synchronously, one
   script per call, no background jobs or `--no-wait`. Check the exit code.
4. Success requires all three postconditions. If a move fails, stop: earlier
   moves may have succeeded (not atomic). Show the full error, inspect
   `05-verify-sql.ps1`, fix the cause, and rerun the same direction (reruns skip
   databases already in the desired state). Do not automatically reverse moves.
5. Report ARM configuration separately from SQL/app readiness. Pooled native
   three-part-name access also needs an Entra server login, mapped users in each
   database, and target-object permissions; validate cross-database reads before
   resuming SQL traffic. The app's "Add these databases to a pool" button only
   displays instructions.

Never run `04-deploy-sql.ps1` or `99-remove.ps1` to switch membership, create a
replacement pool, relax Entra-only auth, add SQL credentials, grant `db_owner`,
or silently change redundancy, tier, or hardware family. Refuse databases that
belong to another pool.

## Safety and truthful reporting

- Never mutate Azure during skill creation or inspection-only work.
- Preview and obtain approval for exact subscription/resources and changes
  before provisioning, publishing, moving, scaling, or resetting compute.
- Read current Microsoft Learn references before changing unfamiliar SQL Server
  2025/Azure SQL syntax or making platform claims. Preserve validated preview
  SQL from this kit; require current onboarding evidence for preview support.
- Keep Entra-only auth and least privilege. Do not expand firewall access or
  change redundancy/region to make a demo work without separate approval.
- Do not run `99-remove.ps1` as a reset, recreate the preview pool, or delete data.
- Source-only, offline-tested, deployed, and live-measured are different states.
  HTTP 200 is not SQL readiness; ARM success is not proof of application recovery.
