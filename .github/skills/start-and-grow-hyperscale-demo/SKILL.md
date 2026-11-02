---
name: start-and-grow-hyperscale-demo
description: 'Deploy, populate, verify, and operate Caldova Patient Access on Azure SQL Database Hyperscale. Use for "Help me deploy a database for Caldova Patient Access", "Populate the database we just deployed with the demo schema and data", "Prepare the resource group and Start logical server", "Reset Scene 2 for a fresh deployment", Caldova sizing or resource reuse, integrating acquired providers, adding or removing provider databases from the elastic pool, pool scaling, measured storage growth, native cross-database queries, or restoring Caldova serverless. Requires explicit approval for Azure changes.'
---

# Start and Grow Hyperscale

Deploy and operate Azure SQL Database Hyperscale for Caldova Patient Access.
Use actual Azure management responses, SQL results, and application measurements.
Keep deployment, database population, compute changes, and data resets separate.

## Safety and interaction

- Never delete either logical SQL server or the shared resource group.
  Preserve server-level cross-database preview settings and the existing pool.
  `99-remove.ps1` is disabled; never use resource-group deletion for a reset.
- Use the currently signed-in Azure CLI user and subscription after read-only
  verification. Do not sign in again, silently switch subscriptions, or ask the
  user to select an already supplied target again. Stop on configuration conflicts.
- Identify resources by subscription display name, resource group, server, pool,
  and database names. Do not display subscription IDs, tenant IDs, tokens, or
  full Azure resource IDs. Redact those identifiers from plans and errors.
- Present one recommended plan with explicit assumptions. For deployment, keep
  the recommendation and rationale within 60 words, followed by at most four
  short change bullets and one approval line. Expand only when requested.
- Use supplied qualitative requirements; do not request numerical usage or cost
  estimates for the pilot. Do not invent sizing guarantees or price estimates.
- Show exact proposed changes and wait for typed approval before execution.
  "Next" is not approval. Approval applies only to the named operation.
  Do not open questionnaires or confirmation forms.
- This environment uses **Allow Azure services**. Include it in the approved
  connection plan; retain existing rules. Do not discover a client IP or create
  a `PresenterClient` rule. This is not a production networking recommendation.
- Keep Entra-only authentication and least privilege. Firewall expansion,
  grants, redundancy changes, and publishing require their own approved scope.
- SQL connections can resume serverless compute, incur charges, and restart the
  idle period. Disclose that before connecting to a paused database.
- Report source, local tests, deployment, ARM state, SQL permissions, and
  application readiness separately. HTTP 200 does not establish SQL readiness.
- If an operation fails, inspect partial completion before rerunning. Surface
  errors explicitly; never infer absence from an authorization/read error.
  Never silently replay an uncertain write.

## Sources and targets

Commands are relative to the repository root. Read
[configuration](../../../startandgrowhyperscale/deploy/00-config.ps1) and the
relevant implementation before issuing commands. Consult the corresponding
setup and measured-evidence sections in the
[README](../../../startandgrowhyperscale/README.md) as needed.
Historical evidence is not current Azure state.

Default targets, subject to configuration and live verification:

| Purpose | Logical server | Database or pool |
|---|---|---|
| Start | `sql-caldova-start-0efc44` | `caldova-scheduling` |
| Grow | `sql-caldova-network-0efc44` | `caldova-scheduling`, `northbrook-scheduling`, `ridgeview-scheduling` |
| Grow pool | `sql-caldova-network-0efc44` | `caldova-network-pool` |

Both servers use `caldova-network-rg`. The same database name on two servers
does not identify the same database. Start deployment must not alter Grow.
Start and Grow can remain provisioned independently; deploying Start does not
require removing Grow databases from the pool.

## Requirements-led database deployment

Recognize this request without requiring a product recommendation in the prompt:

> Help me deploy a database for Caldova Patient Access. Usage peaks in the
> morning, with nights and weekends idle. We need automatic compute scaling,
> low idle cost, and room to grow without sizing storage. A short wake-up
> delay is fine. Use the currently logged-in Azure user and subscription,
> resource group `caldova-network-rg`, and existing server
> `sql-caldova-start-0efc44`. Recommend a configuration, state assumptions,
> and show the plan for approval before deploying.

1. Verify the current CLI account and signed-in Entra user internally. Inspect
   the resource group, Start server, region, security settings, and target
   database through management APIs, without opening SQL connections.
2. Verify current supported configuration and platform limits against Microsoft
   Learn. Recommend Hyperscale for storage growth and serverless for intermittent
   activity and acceptable wake-up delay. For the supplied requirements, propose
   `HS_S_Gen5_8`, 1-8 vCores, and 60-minute auto-pause. Treat local backups,
   no zone redundancy, and Entra-only authentication as pilot assumptions.
   The vCore range is a starting point, not measured capacity assurance.
3. If the named server or resource group is missing, stop and propose the
   separate preparation workflow below. Do not silently substitute the Grow
   server or create additional resources outside the approved plan.
4. Preview the actual deployment:

   ```powershell
   .\startandgrowhyperscale\deploy\09-deploy-part1-sql.ps1 -AllowAzureServices -WhatIf
   ```

   Keep subscription identifiers out of displayed output. Describe resources
   reused versus created, compute, auto-pause, security/connectivity, backup,
   and availability assumptions. Use `-AllowAzureServices` without `-ClientIp`.
   Never use `04-deploy-sql.ps1` for Start; it targets Grow resources.
5. Wait for approval, then run the same command without `-WhatIf`.
   If `caldova-scheduling` exists, the script verifies instead of recreating it.
   Do not call reuse a new deployment, delete the existing database, or change
   mismatched settings without separate approval.
6. Independently verify the exact server/database, status, Hyperscale serverless
   objective, minimum/maximum capacity, auto-pause, and approved security settings.
   Then provide a short verified summary and direct the user to the same
   database in Azure Portal. Population is a separate operation.

## Prepare the Start server

For "Prepare the resource group and Start logical server", verify targets first:

```powershell
.\startandgrowhyperscale\deploy\00-prepare-part1.ps1 -WhatIf
```

Show what will be reused or created and wait for approval before rerunning
without `-WhatIf`. This creates only missing resources and verifies identity,
region, TLS 1.2, public endpoint, and Entra-only authentication. The signed-in
Entra user becomes administrator on a new server. Existing incompatible
servers cause an error rather than silent changes.

No database, firewall rule, schema, app, or pool changes are included. Deployment
separately approves the Allow Azure services setting. Do not use
`04-deploy-sql.ps1` or `99-remove.ps1` for preparation.

## Populate the deployed Start database

For "Populate the database we just deployed with the demo schema and data":

1. Recover and verify the exact Start target from the deployment. Never infer
   it from the shared database name or redirect the multi-provider setup script.
2. Read [the schema](../../../startandgrowhyperscale/sql/01-scheduling-schema.sql).
   Preview creation of missing appointment/slot/provider-directory objects and
   insertion of synthetic future UTC slots and provider entries. No patient
   data, deletion, truncation, booking reset, compute change, or pool move.
   Explain SQL resume implications and obtain approval before connecting.
3. Use the MSSQL extension with the verified Entra deployment identity to execute
   that SQL file only on Start `caldova-scheduling`, without `USE`. Do not run
   `07-deploy-scheduling.ps1 -Phase Schema`, network-view setup, app-login
   creation, or multi-database permission scripts for this single-database step.
4. Verify the connected server/database and all three tables. On a fresh
   database expect 50 unbooked future slots across five specialties, three
   provider keys (`caldova`, `northbrook`, `ridgeview`), and zero appointments.
   On reruns preserve bookings and existing rows; verify no duplicate seed keys.
   Inspect partial state on failure before rerunning.
5. Disconnect setup/verification clients so they do not prevent auto-pause.
   Report actual counts; do not claim acquired-provider setup or cross-database
   access from population of Start.

## Start app and serverless measurements

The app's `part1` launch profile targets the separate Start server. Verify both
the scheduling connection and monitor resource target before launching:

```powershell
dotnet run --project .\startandgrowhyperscale\app\CaldovaNetworkScheduling.csproj --launch-profile part1
```

The profile enables real SQL workload generation. Do not run a workload, resume
compute, or start observation without approval for the target and scope.
Launching and HTTP reachability do not establish application SQL permissions.
Verify reachability before opening a standalone Edge window, not the integrated
VS Code browser.

CPU readings use `sys.dm_db_resource_stats` and each sample's own limit:
`avg_cpu_percent * cpu_limit / 100`. Retain actual UTC timestamps and missing
intervals; workers are not cores. `VIEW DATABASE STATE` may be required;
never grant it automatically.

Billing comes from Azure Monitor, not SQL CPU readings. For average billed
vCores use `app_cpu_billed` Sum divided by bucket seconds. Close all SQL clients
before waiting for the configured idle interval. Verify pause via ARM status
and activity, not SQL polling or absent metric bars. Do not promise CPU-only
billing, zero downtime, fixed resume duration, or measured cache warm-up.
The read-only first-sign-in path can retry transient resume failures; that
exception never permits replaying bookings or pool-scaling writes.

## Pool membership

Use only [the membership script](../../../startandgrowhyperscale/deploy/06-set-pool-membership.ps1):

```powershell
.\startandgrowhyperscale\deploy\06-set-pool-membership.ps1 -Mode Pooled -WhatIf
```

Use `-Mode Standalone -WhatIf` for removal. Inspect current placement first.
The pool must already exist and retain its preview enablement. Reject databases
belonging to another pool. Do not recreate the pool or change preview settings.

- **Pooled:** all three Grow databases join the existing pool; Grow Caldova
  leaves serverless for provisioned Hyperscale compute.
- **Standalone:** restore Grow Caldova to `HS_S_Gen5_8`, minimum 1 vCore,
  auto-pause 60 minutes; Northbrook to `HS_Gen5_2`; Ridgeview to `HS_Gen5_4`.
  Verify current values in configuration.

State actual compute, targets, and possible connection interruptions. Stop
dependent SQL traffic and obtain approval before applying without `-WhatIf`.
Moves are sequential, not atomic. On failure, inspect all three placements
before finishing the same approved direction; do not automatically reverse it.
Keep data, bookings, objects, identities, both servers, and the pool.
Verify every database is Online in the requested placement before resuming SQL.
ARM membership alone does not establish native cross-database SQL readiness.

## Pool scaling and storage growth

- Inspect the existing Ready Hyperscale Gen5 pool and all three Online members.
  Verify capacity support, quotas, redundancy, CLI/API syntax, and limits.
- Cap policy: minimum 0; maximum 12 per database at 24 pool vCores, maximum 8
  at 8 pool vCores. Never exceed total pool capacity. Configuration defines the
  policy; existing mismatches need an approved update, not recreation.
- The real `/scale` controls change 8/24 pool capacity and the corresponding
  per-database maximum, then verify both. These require localhost opt-in.
  Each user click authorizes its defined operation; never click during tests.
  Include cap changes in every assistant-submitted scale approval.
- Approve bounded, cancellable real scheduling/CPU work separately. For
  scale-under-load validation, keep the approved work running through scaling.
  Record timestamps, successes, latency, errors, and recovery. ARM completion
  and application recovery are different evidence. Do not add write retries.
- Actual storage reads use SQL data-file used/allocated measurements. Initial
  Grow fill targets are Caldova 18, Northbrook 12, Ridgeview 15 GiB used via
  `07-deploy-scheduling.ps1 -Phase Storage`, or Schema with `-IncludeStorageFill`.
  Approve this large fill separately; never include it in Start population.
- `PoolGrowth__Enabled=true` permits bounded Caldova inserts past 20 GiB,
  capped at 21 GiB used with a 22-GiB safety ceiling. Stop once SQL confirms
  both used and allocated space above 20 GiB. Keep growth disabled on public
  hosts. Committed data remains after Stop; reruns never reset or shrink it.
- The bounded proof script is `deploy\13-rehearse-pool-growth.ps1`; inspect its
  configuration, preview with `-WhatIf`, and obtain exact approval before running.
  Neither its name nor a successful local test establishes measured growth.

## Native search and booking

Read [the view](../../../startandgrowhyperscale/sql/02-network-view.sql) and
[permissions](../../../startandgrowhyperscale/sql/03-app-permissions.sql).
Use the approved `07-deploy-scheduling.ps1` Network phase for setup; it changes
SQL objects/grants and is not read-only preflight. Inspect partial setup before
rerunning failures. Verify deployed application identity separately from setup identity.

Keep the provider-keyed LEFT JOIN view, Caldova source connection, parameterized
allowlisted provider writes, and conditional single-provider booking transaction.
Preserve membership/future/unbooked checks and concurrency protection.
No external-table substitution, multi-database write transaction, extra audit
write, SQL password, broad role, or uncertain-write replay.

Confirmed bookings remain visible as **Booked** until a fresh availability
search excludes them. The opted-in localhost reset deletes only the exact
confirmed appointment ID and reopens its provider slot in one transaction.
Reset is disabled by default; each explicit click authorizes only that reset.
Never invoke it autonomously or treat a failed/unknown outcome as success.

## Explicit resets

Never infer deletion, reseeding, booking changes, storage shrink, or compute
restoration from an unqualified "reset".

For "Reset Scene 2 for a fresh deployment", first stop dependent Start traffic
and preview deletion of only the explicitly named Start database:

```powershell
.\startandgrowhyperscale\deploy\10-reset-part1.ps1 -ExpectedServerName sql-caldova-start-0efc44 -WhatIf
```

Explain loss of Start schema/data/bookings and preservation of both servers,
resource group, all Grow databases, pool, app, and preview settings.
Wait for exact deletion approval, then run without `-WhatIf`;
`-Confirm:$false` is allowed only after approval. Verify absence independently.
Do not redeploy or populate automatically.

Real Grow storage reset uses `12-reset-demo-storage.ps1` with a separate preview
and approval. It restores only the approved storage payload baseline and must
preserve scheduling data, other objects, memberships, and compute.
Booking resets must identify the exact provider, slot, and appointment.
Capacity restoration and standalone membership restoration are separate operations.

## Validation and execution

Run the smallest relevant offline checks:

```powershell
dotnet build .\startandgrowhyperscale\app\CaldovaNetworkScheduling.csproj --configuration Release
dotnet run --project .\startandgrowhyperscale\tests\CaldovaNetworkScheduling.Checks.csproj --configuration Release
```

For related script changes, run the corresponding mocked checks under
`startandgrowhyperscale\deploy\tests`:
`part1-lifecycle.tests.ps1`, `pool-membership.tests.ps1`,
`pool-deployment.tests.ps1`, or `scheduling-network.tests.ps1`.
New pool deployment must pass explicit min/max limits; reuse mismatches stop
instead of changing or recreating a pool.

The app checks with `-- --live` commit one synthetic Ridgeview booking.
Require explicit target/identity approval; never classify that as read-only.
`05-verify-sql.ps1` checks ARM, `03-verify.ps1` checks HTTP, and scheduling Verify
uses the deployment identity. None substitutes for application-identity SQL proof.

Execute PowerShell operations synchronously, one script per call. Check exit code
and completion. Do not use background deployment jobs or `--no-wait`. If execution
continues after a tool timeout, read that same session rather than rerunning it.
Never claim locally edited source has already been published.
