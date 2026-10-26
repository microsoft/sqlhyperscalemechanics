# Start and Grow Hyperscale: recording script

The exact order of takes for recording the demo. Beat details, presenter lines,
and evidence rules are in [DEMO-RUNBOOK.md](DEMO-RUNBOOK.md). Status reflects
what has actually been proven as of 2026-10-25.

| Status | Meaning |
|---|---|
| **Ready** | Run live end to end (takes 2-6 dry-run on the Part 1 server 2026-10-25) |
| **Rehearse** | Implemented, but not yet run through on camera |
| **Not built** | Do not record; skip or cut |

## Before you record (off camera)

1. On the presenting machine, `az login` to `AzureSQL_bobward`
   (`0efc44aa-c965-420f-aac4-fff305dbcc97`).
2. Confirm the starting state. Part 1: `sql-caldova-start-0efc44` does **not**
   exist yet (take 2 deploys it). Part 2: all three databases on
   `sql-caldova-network-0efc44` standalone and Online (Caldova serverless);
   `caldova-network-pool` empty.
3. For Part 1 (after take 2), start the app locally with `dotnet run --launch-profile part1`
   (targets the Part 1 server, measured load enabled) and open **one** Edge window
   at `http://localhost:5099/`. (The published App Service has load disabled.)
   Stop any older copy of the app already listening on 5099. Use a real Edge
   window, not the VS Code integrated browser, which suspends the page.
4. Open **one** GitHub Copilot chat. Use that same chat for every Copilot step.
5. Close every other SQL client: VS Code MSSQL connections, SSMS, portal query
   editor. Anything connected keeps the database awake and blocks auto-pause.
6. If the skill's GitHub URL will appear on screen, push branch `bw` first.

## Part 1 - Start with Hyperscale

Allow about **90 minutes** of wall-clock time: the database cannot auto-pause
until it has had no connections for at least 60 minutes (Hyperscale minimum).

| Take | What to record | Status |
|---|---|---|
| 1 | **Beat 1.1 - Portal choices.** Portal > Create SQL Database. Show Hyperscale, Serverless vs Provisioned, min/max vCores, region, zone redundancy, networking, Entra auth. Do not click Create. | Rehearse |
| 2 | **Beat 1.2 - Copilot sizing and deployment.** Flip to VS Code. In the Copilot chat, use the runbook prompt and requirements. Copilot recommends Hyperscale serverless 1-8 vCores, 60-min auto-pause, shows the `-WhatIf` plan; approve; it deploys `sql-caldova-start-0efc44` / `caldova-scheduling` and verifies (about 8 min; time-lapse with disclosure). | Ready for the script (deploy ran 7.7 min and verified 2026-10-25; Copilot conversation not yet rehearsed). Server deleted again so the on-camera deploy is real. |
| 3 | **Beat 1.3 - Measured run.** App Step 1, click **Run** (about 10 min). Staff sign in, vCores climb to about 6-7 in under 2 min, hold 1 min, then fall slowly; polling stops on its own. Record in full or as a disclosed time-lapse. | Ready (2026-10-25: peak 7.48 measured vCores ~2.5 min in) |
| - | **Wait (off camera).** Leave the page open; touch nothing that connects to SQL. About 61 minutes after the run ends, Azure pauses the database. | Ready (paused ~60 min after the run) |
| 4 | **Paused.** The page turns grey: "Paused - no compute charges", readout 0.00, "Clinics are quiet". | Ready |
| - | **Wait at least 10 minutes after the pause** before take 5, so the billing chart shows a clear gap. | - |
| 5 | **Resume.** Click **Sign in (resume)**. A staff avatar connects with a spinner; the app retries quietly and shows "First sign-in completed in N s". Status returns to Online. | Ready (resumed in ~15 s; message after a completed run fixed 2026-10-25) |
| - | **Wait 3-5 minutes (off camera)** for Azure Monitor to publish the new billing minutes. | - |
| 6 | **Billing.** In the portal, go to the Caldova database (`sql-caldova-start-0efc44` / `caldova-scheduling`) > Metrics > App CPU billed (Sum), 1-minute granularity, from about 10 min before the run to now. Show the bars: rise with the run, step down slowly, sit at the 1-vCore floor, a gap with no bars while paused, bars return after resume. Don't hover over numbers. | Ready (2026-10-25: rise to 4.47 avg billed vCores, step down, 1.0 floor for 60 min, no bars while paused, bars return after resume) |

Say for take 6: "When the database pauses, compute billing stops - there are no
bars at all - and it picks back up on the next sign-in." Paused minutes have no
billing value; the chart shows a gap, not a zero.

**End of Part 1.** Optional fallbacks: browse to `/serverless-illustration` for the
labeled 70-second illustration (no SQL), or use the backup clips 01-03.

## Part 2 - Grow without redesigning

**Backup clips:** if live cross-database queries are still unavailable, use the
scripted MP4 clips of each app sequence (see
[Playback mode and backup clips](README.md#playback-mode-and-backup-clips)). They
are the real app in playback mode: same pages, clicks, and on-screen SQL, with
in-memory data and no Azure. Record the Azure portal and Copilot parts live, and
disclose in narration that the app segments are scripted.

Can be recorded in a separate session. Do not run Part 1's measured mode while
the databases are pooled.

| Take | What to record | Status |
|---|---|---|
| 7 | **Beat 2.1 - Acquisitions.** App Step 2 (`/network`). Show the three providers, then switch to the Copilot chat for take 8. | Rehearse |
| 8 | **Beat 2.1 - Pool move.** In the same Copilot chat: "Integrate Caldova's acquired providers into the scheduling pool." Copilot shows the preview; confirm no SQL traffic; approve. Caldova leaves serverless; all three move into the pool (several minutes; time-lapse with disclosure). | Rehearse (preflight passed; not yet applied live) |
| 9 | **Beat 2.2 - Network.** In the portal, open `caldova-network-pool`: three databases in the pool. Then app Step 3 (`/verify`) > **Check network**: each database reachable from Caldova, with the query. | **Blocked** - cross-database preview not active on the pool (Msg 40515, 2026-10-24) |
| 10 | **Beat 2.3 - Scale the pool under workload.** | Not built |
| 11 | **Beat 2.4 - Database growth.** | Not built |
| 12 | **Beat 2.5 - Search and book.** App Step 4 (`/appointments`): Cardiology local, then **Search the entire network**; show the view; book a Ridgeview slot; Pediatrics shows Northbrook. | **Blocked** - same as take 9 |

**End of Part 2.**

## Reset (off camera)

In the same Copilot chat: "Return the providers to standalone and restore
Caldova serverless." Caldova returns to serverless 1-8 vCores with 60-minute
auto-pause; Northbrook 2 vCores; Ridgeview 4 vCores. Data, bookings, and the
pool are kept. Booked slots are not reopened.

## Open items before a full recording

1. Push branch `bw`; decide whether to publish this app version to App Service
   (load generation there stays off until access is restricted and the app
   identity's SQL permissions are verified).
2. Rehearse takes 1, 2, 7, and 8 in one Copilot chat.
3. Get the cross-database preview enabled on `sql-caldova-network-0efc44` /
   `caldova-network-pool`, then deploy the view and permissions while pooled and
   verify the app identity (takes 9 and 12). Schema is already in all three databases.
4. Build Beat 2.3 (pool scaling under workload) and Beat 2.4 (measured growth),
   or cut them from this recording.
5. Update the runbook beats for the single Patient Access page and the resume step.
