---
name: start-and-grow-hyperscale-demo
description: 'Build, verify, rehearse, and present the Caldova Start and Grow Hyperscale demo. Use for "Help me deploy a database for Caldova Patient Access", "Populate the database we just deployed with the demo schema and data", "Reset Scene 2 for a fresh deployment", "Prepare the resource group and Start logical server", Caldova sizing or resource reuse, "build the start and grow demo", "update startandgrowhyperscale", "prepare the Mechanics Hyperscale demo", "run the start and grow demo", "rehearse start and grow", starting either chapter, integrating Caldova acquisitions, adding or removing provider databases from the pool, returning providers to standalone, restoring Caldova serverless, or the next scene. One skill and one on-camera chat cover preparation, sizing/deployment, population, database-only reset, Patient Access, pool membership, scaling, growth, and native cross-database queries. Live Azure changes require explicit approval. Excludes unrelated demos.'
---

# Start and Grow Hyperscale

**Never delete either logical SQL server or the shared resource group.**
Preserve server-level cross-database preview enablement. A database-only reset

## Chat interaction

- Do not invoke `ask_user`, `askQuestions`, or questionnaire/choice-box tools
  for this demo, including requirements, resource reuse, sizing, and approval.
- Give one recommended plan in normal chat, with assumptions stated. Do not
  present a menu of deployment options or ask for usage/cost estimates.
- Keep Scene 2 concise: recommendation, rationale, exact changes, approval.
  Keep the recommendation and rationale to at most 60 words, followed by at
  most four short plan bullets and one approval line. No repeated requirements,
  comparison tables, or long technical explanations. For the verified baseline:
  "I recommend Hyperscale serverless: compute scales with your morning demand,
  pauses when idle, and storage grows automatically. Start with 1-8 vCores and
  60-minute auto-pause."
  The plan bullets identify the verified resource group/server/database,
  security/connectivity, backup/availability assumptions, and what is preserved.
  Use actual inspected values; if blocked, give the blocker instead of this
  success-path recommendation. Expand only when Bob asks.
  Do not append generic cost caveats about CPU/memory billing, minimum charges,
  open SQL sessions, absent monetary estimates, pricing links, or documentation
  verification dates. Keep technical checks internal; explain billing and
  auto-pause prerequisites when those later scenes require them, or when asked.
- This demo uses **Allow Azure services**. Include that setting in the approved
  connection plan, retaining it if already enabled. Do not discover a presenter
  IP or create a `PresenterClient` rule. This is a demo-specific choice, not a
  production networking recommendation; do not remove existing rules.
- Use the supplied targets and current Azure CLI identity/subscription after
  read-only verification. Do not ask Bob to select them again.
- For approval, display the exact proposed changes and stop. Wait for Bob to
  type approval in chat; do not open a confirmation form.
- If an essential target or safety requirement cannot be established, explain
  the blocker in chat and stop rather than opening a questionnaire or guessing.
- These interaction rules do not waive Azure change approval or bypass VS Code's
  own tool-permission controls. A database-only reset
must not remove or recreate its logical server, even indirectly through resource
group deletion. `99-remove.ps1` is disabled for this reason. If a server appears
absent, stop and confirm the intended subscription/resource group/server before
proposing creation; a new server does not inherit preview enablement.

Help Bob build the agreed demo correctly, prove each capability, and present it
one beat at a time. There are two independently presentable chapters:

1. **Start:** Portal choices -> Copilot-assisted sizing/deployment ->
   portal verification -> serverless range and Caldova Patient Access -> billing evidence.
2. **Grow:** acquisitions -> serverless to elastic pool -> pool scaling under
   workload and measured database growth -> cross-database queries.

## Canonical sources

All command paths below are relative to the `sqlhyperscalemechanics` repository root.

- [Runbook](../../../startandgrowhyperscale/DEMO-RUNBOOK.md):
  locked chapter/beat order, presenter cues, evidence gates, and reset.
- [Recording script](../../../startandgrowhyperscale/RECORDING-SCRIPT.md):
  scene-by-scene recording order and current readiness.
- [README](../../../startandgrowhyperscale/README.md):
  current implementation, setup, commands, validation, and measured evidence.
- [Configuration](../../../startandgrowhyperscale/deploy/00-config.ps1):
  resource names, subscription selection, compute, and redundancy.
- [Pool membership script](../../../startandgrowhyperscale/deploy/06-set-pool-membership.ps1):
  canonical Pooled/Standalone implementation; see "Pool membership" below.
- [Start preparation](../../../startandgrowhyperscale/deploy/00-prepare-part1.ps1):
  create the resource group and Start logical server off camera, without a database.
- [Scene 2 reset](../../../startandgrowhyperscale/deploy/10-reset-part1.ps1):
  delete only the Start database, preserving servers and all Grow resources.
- [Grow storage reset](../../../startandgrowhyperscale/deploy/12-reset-demo-storage.ps1):
  restore only network Caldova dummy data to 18 GiB used / 20 GiB allocated.

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
  the explicit Scene 2 database-only reset below, restoring an approved pool
  capacity, and changing synthetic bookings.
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
3. Use the "Scene 2" interaction below for Part 1 deployment:
   - Use the supplied qualitative workload, growth, idle, warm-up, and budget
     requirements. Do not request numerical usage, concurrency, storage, or cost
     estimates for this pilot. Keep unknown capacity explicit and state proposed
     defaults as assumptions in the approval plan, not measured requirements.
   - Recommend a supported starting configuration, including serverless min/max
     and auto-pause suitability. The existing 1-8 vCore range is the demo
     baseline, not a sizing formula. Do not invent price or capacity estimates.
   - Verify current options, limits, and billing against Microsoft Learn.
   - Present a change-management summary: inputs, assumptions, sizing rationale,
     exact target/resources, compute range, availability/network choices,
     proposed actions, verification,
     and rollback limits. Missing inputs remain explicitly unresolved.
   - The skill/repository to show is this skill at
     `https://github.com/microsoft/sqlhyperscalemechanics/tree/main/.github/skills/start-and-grow-hyperscale-demo`.
     The repository is private; confirm the intended branch, that it is
     published, and that it is acceptable on camera before showing the link;
     do not imply uncommitted local changes are already on GitHub.
   - Before deployment, show the exact plan and obtain approval. The Part 1
     deployment is `deploy\09-deploy-part1-sql.ps1`: run it with `-WhatIf` to
     show the plan (server `sql-caldova-start-*`, database `caldova-scheduling`,
     `HS_S_Gen5_8` 1-8 vCores, auto-pause 60, Entra-only, local backups, no zone
     redundancy, firewall), then run it without `-WhatIf` after Bob approves.
     Use `-AllowAzureServices` without `-ClientIp` for this demo; include that
     setting in the approved plan, not a presenter-IP firewall rule.
     Never use `04-deploy-sql.ps1` for
     this beat; it creates the Part 2 server, three databases, and a pool.
   - The runbook's Beat 1.2 lists the requirements Bob will give and the
     recommendation they lead to. Derive the recommendation from his answers and
     the capability limits (with max 8 vCores the lowest allowed minimum is 1);
     if his answers differ, say how the recommendation changes.
4. Separate the app's chapter entry points and navigation without losing the
   existing routes or telemetry/SQL disclosures. Part 1 must end independently;
   Part 2 must be directly accessible after its own preflight.
5. Keep pool membership in `deploy\06-set-pool-membership.ps1` and the
   "Pool membership" section below. Stop dependent SQL
   traffic while databases move. Retain the existing preview-enabled pool.
6. Implement pool scaling as a separate approved operation on that same pool:
   - **Persistent pool cap policy:** use minimum 0 and maximum 12 vCores per
     database for the 24-vCore demo pool. For an 8-vCore pool, use maximum 8;
     never configure a per-database maximum above total pool capacity.
     `deploy\00-config.ps1` defines the policy. New pool deployment must pass
     explicit minimum/maximum values, not rely on Azure defaults. Existing
     pools must be inspected; mismatched limits require an exact approved
     change, never pool recreation. The `/scale` controls set maximum 8 when
     scaling to 8 and restore maximum 12 when scaling to 24, then verify both
     capacity and limits. Include those cap changes in every scale approval.
     A twelve-core cap allows unequal bursts while preventing one database
     from consuming all twenty-four cores. It applies even when peers are idle;
     eight is not an inherent Hyperscale per-database limit or a fixed partition.
     Editing this policy is not approval to apply it to the current live pool.
     Reuse the pool and preserve preview settings, data, and memberships.
     The recording scene should show uneven demand (for example 12/7/5 in
     the 24-core pool), not force every database to eight. Show shared capacity
     and per-database minimum/maximum separately from CPU-used readings.
     The illustration models the policy, not proof of current live limits.
   - Bob explicitly selected a thirty-second recording illustration for this
     beat. `/scale-presentation` with `PoolPresentation__Enabled=true` animates
     CPU and 20-to-30-GiB allocation in memory, with no on-screen speed-up
     caption. Its controls never call Azure or SQL and never book. Do not claim
     these values are live SQL telemetry. Keep `/scale` as the distinct real
     path and presentation off on public hosts; all opt-ins require localhost.
     Use `deploy\13-rehearse-pool-growth.ps1 -IncludeGrowth` for real off-camera
     proof, previewing with `-WhatIf` and obtaining explicit operation approval.
     It uses the current CLI identity and verifies actual samples and growth,
     never resetting or changing compute. Do not run real load merely to test
     the presentation. Repeat presentation takes reset only the in-memory model.
   - Current local implementation: Step 4 `/scale` has 24/8-vCore controls and
     bounded read-only scheduling/CPU work with two immediate workers per database,
     ramping in groups of two to nine within twenty seconds to target eight
     CPU-used cores per database. Worker counts are not cores and do not change
     provisioned capacity. Only six
     workers start immediately so the first SQL sampling interval includes rising
     load. Keep actual 15-second averages; no smoothing or invented values.
     Fifteen-second holds and three-worker ramp-down steps give three 45-second profiles.
     The optional stopping-group override leaves other callers' ramp behavior unchanged.
     The revised curve and peak require live rehearsal. Short 100,000-iteration scalar batches target near 24
     measured CPU-used cores, requiring each database to approach eight briefly;
     worker counts are not cores or proof of a peak. Each explicit button click authorizes its defined operation without
     another confirmation; do not click or invoke it autonomously during tests.
     Controls require localhost-only opt-in, apply the pool cap policy above,
     and verify the current target/account. Source and offline checks are ready;
     live scale-under-load behavior still needs rehearsal. The shorter CPU scene
     plans for 60 seconds including 15-second cooldown, excluding startup and
     overrunning SQL. Errors distinguish CPU workload, CPU reads, and storage
     reads; no retry or fabricated data. Step 3 `/verify`
     is resource inspection only; Search & Book is Step 5.
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
7. Scale now uses real SQL used/allocated data-file measurements, not fictional
   growth. Initial Grow seeding targets Caldova 18, Northbrook 12, Ridgeview 15
   GiB used through `deploy\07-deploy-scheduling.ps1 -Phase Storage`; Schema can
   opt in with `-IncludeStorageFill`. Use the current CLI identity/subscription,
   require all three Online in the existing pool, and approve this large fill
   separately. Never include it in Part 1 population. Local opt-in
   `PoolGrowth__Enabled=true` changes Run to **Run Scheduling & Grow**: its click
   authorizes bounded synthetic Caldova inserts past 20 GiB, capped at 21 GiB, while all clinics run
   CPU work. Growth is capped at 45 seconds, followed by 15 seconds of
   observation. Stop growth once SQL confirms both used and allocated space above
   20 GiB; do not continue chasing the cap after the allocation change. Already-grown
   storage changes the button to Run Scheduling and is preserved. Use moderately
   larger cards with measured CPU values and simple storage bars, not CPU charts.
   Another allocation-change take requires a separately approved reset. The SQL
   safety ceiling remains 22 GiB. For recording, recommend a separately approved
   off-camera pre-fill to 19.75 GiB used; it has not been performed and does not
   replace the 18/12/15 initial seed targets. Do not autonomously click during
   checks. Dedicated uncompressed payload batches preserve scheduling data.
   Stop joins activity and refreshes measurements; committed data remains.
   Reruns measure remaining space, never reset/shrink/replay uncertain writes.
   Keep growth disabled on public/playback hosts; verify deployed-app SQL access
   separately. Offline tests, initial live fill, crossing 20 GiB, and actual
   near-24 CPU peak are separate evidence gates. Explain independent storage
   without implying unlimited I/O or wholly compute-independent limits.
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

For pool creation or cap-policy changes, also run the mocked deployment checks:

```powershell
& .\startandgrowhyperscale\deploy\tests\pool-deployment.tests.ps1
```

These checks verify explicit caps on creation and preserve existing pools;
mismatched limits stop rather than changing or recreating the pool.

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

## Scene 2 - Requirements-led deployment

Recognize this prompt without requiring Bob to name Hyperscale, serverless,
or the skill:

> Help me deploy a database for Caldova Patient Access. Usage peaks in the
> morning, with nights and weekends idle. We need automatic compute scaling,
> low idle cost, and room to grow without sizing storage. A short wake-up
> delay is fine. Use the currently logged-in Azure user and subscription,
> resource group `caldova-network-rg`, and existing server
> `sql-caldova-start-0efc44`.
> Recommend a configuration, state assumptions, and show the plan for approval
> before deploying.

1. Use the existing Azure CLI session: inspect `az account show` and the
   signed-in user rather than asking Bob to choose a user, tenant, or subscription.
   Do not sign in again or silently switch subscriptions. Check configuration
   overrides against that current subscription; stop and explain any conflict.
   For this prompt, reuse the named resource group/server after read-only checks,
   without a reuse questionnaire. Follow "Chat interaction" for every step:
   no choice boxes, questionnaires, or numerical estimate requests.
   Present assumptions and the approval plan in
   chat, then wait for Bob's explicit approval before applying any changes.
   Use the inspected server region; propose local backups, no zone redundancy,
   and Entra-only authentication as pilot assumptions, not supplied requirements.
2. Inspect the selected subscription, resource group, server, and target
   database through Azure management APIs without opening SQL connections.
   Verify existence, region, access, and compatibility with the proposed plan.
   If a named resource is absent, disclose that and ask before planning its
   creation. The Part 1 deployment script requires an existing resource group;
   use the separately approved off-camera preparation below if it is missing.
3. Recommend Hyperscale for the requested storage-growth experience and
   serverless for intermittent activity, automatic compute scaling, idle cost,
   and acceptable wake-up delay. Explain the tradeoffs; do not present this as
   a measured capacity guarantee. With the confirmed pilot requirements,
   propose `HS_S_Gen5_8`, 1-8 vCores, and 60-minute auto-pause, subject to
   current regional support. If requirements differ, adapt the recommendation.
4. Confirm `caldova-scheduling` as the database name. Use the confirmed targets
   in `deploy\00-config.ps1`; nondefault existing targets can be selected with
   `AZURE_SUBSCRIPTION_ID`, `CALDOVA_NETWORK_RESOURCE_GROUP`, and
   `CALDOVA_PART1_SQL_SERVER`. Do not accidentally target the Grow server.
   If the database exists, explain that the script skips creation and verifies
   its compute settings. Never call that a new deployment, delete/recreate it,
   or change a mismatched database without separate approval.
5. Show the exact `09-deploy-part1-sql.ps1 -WhatIf` plan, identifying resources
   reused versus created and any firewall/authentication changes. Reusing a
   server does not prove its existing security settings match the plan.
   Obtain approval before applying; do not execute creation until compatibility
   and the exact operation scope are confirmed. If new-resource provisioning
   is outside this script's capabilities, stop and obtain an explicit plan.
6. Verify the approved result, then cue Scene 3: open that same database in
   Azure Portal and show its server/name, Hyperscale serverless configuration,
   1-8 vCore range, and 60-minute auto-pause before starting Patient Access.

## Off-camera - Prepare Scene 2 resources

Prompt: "Prepare the resource group and Start logical server for Scene 2.
Do not create a database. Show the plan and wait for approval."

Confirm subscription, resource group, and Start server names from configuration.
Run `deploy\00-prepare-part1.ps1 -WhatIf` with the full repository-relative path,
show which resources will be reused/created, and obtain approval before rerunning
without `-WhatIf`. This creates only missing resources and verifies the server's
identity, region, TLS 1.2, public endpoint, and Entra-only authentication.
Existing incompatible servers cause an error rather than silent changes.
The signed-in Entra user becomes administrator on a newly created server.
No database, firewall rules, schema, app, or pool changes are included;
Scene 2 deployment separately previews/approves the Allow Azure services setting.
Never use `04-deploy-sql.ps1` or `99-remove.ps1` for this preparation.

## Off-camera - Reset Scene 2

Recognize this explicit prompt:

> Reset Scene 2 for a fresh deployment. Delete only `caldova-scheduling` on
> `sql-caldova-start-0efc44`. Keep the resource group, both servers, and all
> databases on the network server. Show the plan and wait for approval.

1. Stop dependent Start app/workload traffic with approval. Verify the prompt's
   exact server agrees with configuration; do not silently substitute a target.
   The script refuses the Grow server and servers outside `sql-caldova-start-*`.
2. Preview from the repository root:
   ```powershell
   .\startandgrowhyperscale\deploy\10-reset-part1.ps1 -ExpectedServerName sql-caldova-start-0efc44 -WhatIf
   ```
   State the subscription and full database resource ID. Deletion removes all
   schema, seed data, and bookings in that Start database. Servers, resource
   group, all Grow databases (including Grow Caldova), pool, and app are kept.
   This is not the provider standalone operation. Never use `99-remove.ps1`.
3. Wait for explicit approval of that exact deletion, then run the same command
   without `-WhatIf`, adding `-Confirm:$false` only after that approval. Run
   synchronously; the script verifies absence after deleting. Already absent
   is a no-op; read/authorization errors are failures, not evidence of absence.
4. Report deletion verification separately from deployment readiness.
   Do not automatically redeploy, seed data, move providers, or reopen bookings.
   Next, run Scene 2 deployment when approved, then the separate off-camera
   population workflow. A generic "reset" is not deletion approval.

## Off-camera - Populate the deployed database

Recognize this follow-up in the same deployment chat:

> Populate the database we just deployed with the demo schema and data.
> This is off camera.

This is a separate approved setup operation after deployment/portal verification
and before Scene 4, not part of the on-camera deployment or a measured growth beat.

1. Recover the exact subscription, resource group, server, and database verified
   in Scene 2. Confirm them through Azure management APIs. If context is missing,
   ask for the target; never infer the server from the shared database name.
   The default Part 1 target is `sql-caldova-start-0efc44` /
   `caldova-scheduling`, not the Grow server.
2. Read `sql\01-scheduling-schema.sql`. Preview its scope: create missing
   `dbo.AppointmentSlot`, `dbo.Appointment`, and `network.ProviderClinic`
   objects/indexes and insert missing synthetic future UTC slots and provider
   directory entries. No real patient data, deletion, truncation, booking reset,
   compute changes, or pool moves. Explain that SQL access may resume compute,
   incur charges, and restart the idle period; obtain approval before connecting.
3. Use the MSSQL extension with the confirmed Entra deployment identity to
   execute that existing SQL file on a connection explicitly targeting only
   the deployed `caldova-scheduling` database, without `USE`. Do not run
   `07-deploy-scheduling.ps1 -Phase Schema` for this single-database step:
   it targets the Grow server, creates an app login, and loops over all three
   providers. Do not redirect that multi-database script to the Start server.
   Do not run network-view or app-permission scripts as part of population.
4. Verify the connected server/database and all three tables. Report total,
   future, and unbooked slot counts by specialty, appointment count, and the
   provider directory keys. On a fresh database the existing seed creates
   50 unbooked future slots across five specialties, three provider rows
   (`caldova`, `northbrook`, `ridgeview`), and zero appointments.
   On reruns verify the current seed's dates/keys exist without duplicates;
   preserve existing rows and bookings rather than requiring fresh counts.
   Surface any SQL failure and stop; setup is not atomic, so inspect partial
   completion before rerunning. Never report success from script exit alone.
5. Disconnect setup/verification clients afterward so they do not prevent
   auto-pause. Report the verified target and counts, then return to the next
   scene. This populates only the deployed database; it does not provision
   acquired-provider databases, establish cross-database access, or change
   the current separate-server arrangement for the Grow chapter.

## Present one scene at a time

Use "scene" in presenter-facing cues. Scene 1 is Portal choices, Scene 2 is
requirements-led deployment, and Scene 3 verifies that deployment in the portal.
Skip rehearsing Scene 1 when requested; do not renumber it. The recording script
maps later scenes to the runbook's existing beat identifiers.

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
   Bob's 2026-10-29 app target is a four-and-a-half-minute planned sequence:
   start the first worker as soon as the database connection is ready, without
   an idle baseline, then ramp to 8 and back to 0 with 10-second steps and
   55-second hold, followed by 75-second cooldown. Startup/connection time may
   add to elapsed time. Stop cancels workload and SQL observation immediately,
   retaining collected samples and returning the controls to idle.
   Rehearse this shorter sequence before claiming its peak is verified.
   A peak of 6-7 CPU-used vCores
   is acceptable. Patient Access is one page with Scripted (`/serverless-illustration`)
   and Measured (`/`) modes sharing the staff/KPI/schedule scene and vCore panel;
   measured staff online equals active SQL workers and patient entries are
   labeled illustrative. Measured mode shows Paused/Resuming from Azure
   management status (no SQL) and has a **Sign in (resume)** button. Bob decided
   (2026-10-23) that this read-only first sign-in retries transient errors
   such as 40613 automatically so no error appears on stage; this exception
   covers only that read-only resume sign-in, not bookings or pool scaling.
   In load-enabled demos, successful sign-in automatically starts four real SQL
   workers for 30 seconds, followed by 75 seconds of DMV observation. This
   approved bounded activity makes post-resume billing visible; failed sign-in
   or disabled load must never start it. Keep prior CPU samples and actual gaps.
   Billing comes from Azure Monitor, not these SQL CPU samples; wait for its
   ingestion and verify the actual bars rather than guaranteeing a particular
   height from a 30-second burst.
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
   compute. Explain the compute change and possible connection interruptions.
   Part 1 measured mode runs against the separate Part 1 server
   (`sql-caldova-start-*`, launch profile `part1`), so pool moves on the Part 2
   server do not affect it. Ask for plain-text approval and wait. Stop dependent app/workload
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
   resuming SQL traffic. Pool membership changes happen only through this skill;
   the local Scale page changes the existing pool's capacity, not membership.

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
