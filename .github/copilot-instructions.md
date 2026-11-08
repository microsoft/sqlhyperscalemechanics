# Chat interaction

- Use normal chat, not dialog boxes, choice boxes, questionnaires, `ask_user`,
  or `askQuestions`, for requirements, recommendations, or approvals.
- Present one recommended plan with explicit assumptions rather than a menu
  of options. If essential information is missing, explain the blocker in chat.
- For changes requiring approval, show the exact plan and wait for the user to
  type approval. Do not replace approval with a form or infer it from "next".
- These preferences do not bypass tool-permission controls.

# Opening the app

- When asked to load, run, open, or relaunch the app, verify it is reachable,
  then open it in a separate standalone Microsoft Edge window (`msedge.exe --app=<URL>`).
- Never use the VS Code integrated browser or Simple Browser for these requests.

# Azure identifier privacy

- Do not display subscription IDs or tenant IDs in chat, plans, summaries,
  copied commands, or screenshots intended for recording.
- Verify and use those IDs internally as needed. Limit tool output with explicit
  queries so account objects and full Azure resource IDs do not expose them.
- Identify approved targets by subscription display name, resource group,
  server, and database name. Redact subscription/tenant IDs from errors and
  resource paths before presenting them; retain the actionable error details.

# Start and Grow demo

- Follow [.github/skills/start-and-grow-hyperscale-demo/SKILL.md](skills/start-and-grow-hyperscale-demo/SKILL.md)
  for Caldova deployment, rehearsal, population, and reset.
- Use the currently logged-in Azure CLI user and subscription after read-only
  verification. Do not ask the user to select them again or silently switch.
- Use supplied qualitative requirements; do not request numerical usage or cost
  estimates for the pilot. State sizing assumptions in the deployment plan.
- Never delete either demo logical SQL server or the shared resource group.
  Preserve their server-level cross-database preview settings.
- Scene 2 reset deletes only the explicitly approved Start database. Keep all
  databases on the network server, both logical servers, and the pool.
