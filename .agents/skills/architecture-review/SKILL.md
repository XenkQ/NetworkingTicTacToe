---
name: architecture-review
description: "Use when: reviewing a system change for DI correctness, client-server ownership boundaries, network protocol safety, turn-flow determinism, and architecture drift risks."
---

# Architecture Review Skill

Use this skill to audit a proposed or implemented system change against Networking Tic-Tac-Toe architecture constraints, ASP.NET Core DI conventions, SignalR protocol safety, and Phaser 4 client lifecycle rules.

## Required Sources

Always read these before reviewing:

- AGENTS.md
- .agents/context/project-coding-standards.md
- .agents/context/ai-game-dev-best-practices.md
- .agents/context/technology-documentation.md

## Inputs

- Target system or feature name.
- Changed files or planned touch points (`server/`, `client/`, `tests/`).
- Intended behavior change.

## Review Boundary

An architecture review produces findings and a verdict; it does not authorize fixes. Reuse explicit implementation authority when present and follow the durable plan/requirements gate before non-trivial code edits. Report unrelated baseline failures without expanding the repair scope. Consult the official sources in .agents/context/technology-documentation.md before relying on framework behavior.

## Severity Classification

Group every finding into one of four severity tiers:

- Blocker: Must be resolved before merge/commit.
  - Broken C# or TypeScript compilation, compiler warnings, or failing xUnit tests.
  - Client-trusted game rules (allowing client to dictate win state, turn order, or unvalidated move coordinates).
  - Unregistered DI service in `server/Program.cs` or static mutable singleton state (`public static Instance`).
  - SignalR transport dependencies (`Microsoft.AspNetCore.SignalR`) leaking into pure domain models under `server/Domain/`.
  - Direct `HubConnectionBuilder` instantiation inside a Phaser Scene instead of `client/src/services/`.
  - Client-server DTO contract mismatch (property names, types, or JSON casing mismatch between `server/DTOs/` and `client/src/types/`).
- Major: Serious architecture, concurrency, or lifecycle flaws that need resolution.
  - Unsynchronized concurrent mutation of shared session/matchmaking state across parallel SignalR requests.
  - Missing disconnect handling (`OnDisconnectedAsync`) leaving orphaned sessions or stuck opponents.
  - Phaser Scene subscribing to `NetworkService` or global events without unsubscribing on scene `SHUTDOWN` / `DESTROY`.
  - Missing xUnit test coverage for new or modified domain rules or win/draw/move evaluations.
- Minor: Standards violations and non-blocking structural drift.
  - Explicit `any` in TypeScript or missing strict return/parameter types.
  - C# methods using expression-bodied arrow syntax (`=>`) instead of `{}` block bodies.
  - Private field naming not matching `_camelCase` in C# or constants not using `UPPER_SNAKE_CASE`.
  - Non-colocated narrow interface owned by a single C# service implementation.
- Nit: Stylistic suggestions and code clarity notes.
  - Minor variable renaming, formatting, or local comment clarity.

## Full-Stack Breaking Change Matrix

Check changed files against the following breaking dimensions:

1. Network Protocol & DTO Breaking:
   - Renaming, removing, or changing types of properties in `server/DTOs/` without updating matching TypeScript interfaces in `client/src/types/` and hub handlers in `client/src/services/`.
   - Changing SignalR Hub method names or parameter signatures without updating all client `invoke` / `on` calls.
2. DI & Service Lifetime Breaking:
   - Changing constructor dependencies without updating registration in `server/Program.cs`.
   - Injecting `Scoped` or `Transient` stateful services where a thread-safe `Singleton` session store is required (or vice versa).
3. Turn Flow & Event Ordering Breaking:
   - Altering SignalR broadcast order (e.g. `MoveMade`, `GameOver`, `RematchStarted`) in a way that causes client UI race conditions.
   - Failing to reset board/turn state cleanly on rematch.

## Review Workflow

1. Identify impacted layers (`server/Domain`, `server/Services`, `server/Hubs`, `client/src/services`, `client/src/scenes`, `tests/`).
2. For implemented changes, run verification checks (`dotnet build`, `dotnet test`, `npm --prefix client run build`, and `.agents/scripts/check-architecture.ps1`). For a proposed design, list them as future checks. Report executed checks, baseline diagnostics, and unavailable checks separately.
3. Check ASP.NET Core DI registration, domain purity, and thread safety.
4. Check Phaser scene event unsubscriptions and TypeScript strictness.
5. Apply the Full-Stack Breaking Change Matrix.
6. Classify findings into Blocker, Major, Minor, and Nit.
7. Issue a clear verdict:
   - Approve: 0 Blocker, 0 Major findings.
   - Request Changes: 1+ Blocker or Major findings.
   - Pending Verification: required checks could not run; do not present them as passed.

## Output

Produce a filled review based on:

- .agents/skills/architecture-review/templates/architecture-review-checklist.md
