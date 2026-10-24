# Start and Grow Hyperscale: recording script

The exact order of takes for recording the demo. Beat details, presenter lines,
and evidence rules are in [DEMO-RUNBOOK.md](DEMO-RUNBOOK.md). Status reflects
what has actually been proven as of 2026-10-23.

| Status | Meaning |
|---|---|
| **Ready** | Run live end to end on 2026-10-23 |
| **Rehearse** | Implemented, but not yet run through on camera |
| **Not built** | Do not record; skip or cut |

## Before you record (off camera)

1. On the presenting machine, `az login` to `AzureSQL_bobward`
   (`0efc44aa-c965-420f-aac4-fff305dbcc97`).
2. Confirm the starting state: all three databases standalone and Online;
   `caldova-scheduling` serverless 1-8 vCores, 60-minute auto-pause;
   `caldova-network-pool` empty.
3. Start the app locally with measured load enabled and open **one** Edge window
   at `http://localhost:5099/`. (The published App Service has load disabled.)
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
| 2 | **Beat 1.2 - Copilot sizing.** In the Copilot chat, use the runbook prompt. Copilot asks requirements, recommends a configuration, and shows a change summary. Do not deploy; say the database already exists from this plan. | Rehearse |
| 3 | **Beat 1.3 - Measured run.** App Step 1, **Measured**, click **Run** (about 10 min). Staff sign in, vCores climb to about 6-7 in under 2 min, hold 1 min, then fall slowly; polling stops on its own. Record in full or as a disclosed time-lapse. | Ready |
| - | **Wait (off camera).** Leave the page open; touch nothing that connects to SQL. About 61 minutes after the run ends, Azure pauses the database. | Ready |
| 4 | **Paused.** The page turns grey: "Paused - no compute charges", readout 0.00, "Clinics are quiet". | Ready |
| 5 | **Resume.** Click **Sign in (resume)**. A staff avatar connects with a spinner; the app retries quietly and shows "First sign-in completed in N s". Status returns to Online. | Ready |
| - | **Wait 3-5 minutes (off camera)** for Azure Monitor to publish the new billing minutes. | - |
| 6 | **Billing.** Click **Open Caldova database in Azure portal** > Metrics > App CPU billed (Sum), 1-minute granularity, from about 10 min before the run to now. Show the bars: rise with the run, step down slowly, sit at the 1-vCore floor, a gap with no bars while paused, bars return after resume. Don't hover over numbers. | Ready |

Say for take 6: "When the database pauses, compute billing stops - there are no
bars at all - and it picks back up on the next sign-in." Paused minutes have no
billing value; the chart shows a gap, not a zero.

**End of Part 1.** Optional fallback: the **Scripted** toggle plays the
labeled 70-second illustration (no SQL) if a live take fails.

## Part 2 - Grow without redesigning

Can be recorded in a separate session. Do not run Part 1's measured mode while
the databases are pooled.

| Take | What to record | Status |
|---|---|---|
| 7 | **Beat 2.1 - Acquisitions.** App Step 2 (`/network`). Show the three providers; click **Add these databases to a pool** (instructions only). | Rehearse |
| 8 | **Beat 2.1 - Pool move.** In the same Copilot chat: "Integrate Caldova's acquired providers into the scheduling pool." Copilot shows the preview; confirm no SQL traffic; approve. Caldova leaves serverless; all three move into the pool (several minutes; time-lapse with disclosure). | Rehearse (preflight passed; not yet applied live) |
| 9 | **Beat 2.2 - Verify.** App Step 3 (`/verify`) > **Open pool in Azure portal**: three databases in `caldova-network-pool`. Then **Check cross-database reads** in the app. | Rehearse (SQL login/user setup must be verified first) |
| 10 | **Beat 2.3 - Scale the pool under workload.** | Not built |
| 11 | **Beat 2.4 - Database growth.** | Not built |
| 12 | **Beat 2.5 - Search and book.** App Step 4 (`/appointments`): Cardiology local, then **Search the entire network**; show the view; book a Ridgeview slot; Pediatrics shows Northbrook. | Rehearse (live app-identity verification pending) |

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
3. Verify cross-database SQL setup and the app identity (takes 9 and 12).
4. Build Beat 2.3 (pool scaling under workload) and Beat 2.4 (measured growth),
   or cut them from this recording.
5. Update the runbook beats for the single Patient Access page and the resume step.
