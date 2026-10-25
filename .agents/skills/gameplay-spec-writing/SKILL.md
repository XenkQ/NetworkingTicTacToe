---
name: gameplay-spec-writing
description: "Use when: writing or reviewing technical specifications and implementation plans for new gameplay features, board rules, matchmaking flows, SignalR protocols, or Phaser UI flows to staff-engineer standards before implementation starts. Triggers: write spec, feature spec, gameplay spec, technical specification, design doc, implementation breakdown."
---

# Gameplay Spec Writing Skill

Use this skill to design and specify new gameplay features, board mechanics, matchmaking systems, SignalR network protocols, or Phaser 4 UI flows to staff-engineer standards before implementation begins. It enforces a skeleton-first approach with a hard Open Questions gate to ensure architectural purity, contract safety, and zero gameplay regressions.

## Required Sources

Before writing a spec, load context from:

- AGENTS.md
- .agents/context/project-coding-standards.md
- .agents/context/ai-game-dev-best-practices.md
- .agents/context/technology-documentation.md
- Relevant game system docs under .agents/context/game-systems/
- Relevant brainstorm summaries under .agents/context/brainstorming-summaries/
- Existing implementation plans under .agents/context/implementations/plans/

## Workflow

1. Load Context & Scope
   - Identify touched layers: `server/Domain/`, `server/Services/`, `server/Hubs/`, `server/DTOs/`, `client/src/services/`, `client/src/scenes/`, `client/src/ui/`, and `tests/`.
   - Inspect existing interfaces, DTO records, services, and Phaser scenes to prevent duplicate abstractions.
   - Reuse the user's approved design choices, prior answers, and existing plan. Do not reopen resolved decisions unless new evidence creates a consequential conflict.

2. Draft Minimal Skeleton Spec
   - Save the skeleton directly under .agents/context/implementations/plans/[feature-name]-spec.md before presenting it: feature summary, architectural intent, resolved decisions, and critical unknowns. Update an existing canonical spec rather than creating competing plans.
   - Do not write the full detailed spec in one pass without validating assumptions.
   - Include a numbered Open Questions block for critical game rule, matchmaking, protocol, or UX unknowns.

3. Open Questions Hard Gate
   - If consequential unknowns remain, present the saved skeleton and ask only those questions; pause dependent design/code work while continuing independent inspection. Routine reversible technical choices do not require repeated approval.
   - Resolve decisions that would otherwise force rewriting architecture or network contracts:
     - Game rules, turn order, and rematch policy.
     - SignalR protocol payloads and error/rejection semantics.
     - Disconnect/abandonment lifecycle behavior.
   - Once questions are resolved, proceed to the detailed design.
   - If all consequential choices are already resolved, record Open Questions: None and proceed to the detailed spec using existing authorization. A request to write a spec authorizes the document, not runtime implementation; preserve the before-code requirements gate in AGENTS.md for later implementation.

4. Detailed Design & Architecture
   - Define Domain Model (`server/Domain/`): pure classes, structs, enums, deterministic state transitions.
   - Define Network Contracts (`server/DTOs/` & `client/src/types/`): immutable C# `record` types and matching TypeScript interfaces.
   - Define SignalR Hub & Service Wiring (`server/Hubs/`, `server/Services/`, `server/Program.cs`): DI lifetimes (`Singleton` vs `Scoped`), thread safety, hub RPC signatures.
   - Define Client Architecture (`client/src/services/`, `client/src/scenes/`, `client/src/ui/`): `NetworkService` methods, Phaser scene lifecycle, visual states, and event cleanup on `SHUTDOWN`.
   - Define Guardrails & Invariants:
     - Authoritative server validation (rejecting illegal moves cleanly).
     - Concurrency & disconnect safety.
     - Strict TypeScript typing (zero `any`) and C# coding standards.

5. Implementation Breakdown (Phases & Steps)
   - Break down the work into discrete Phases (stories) and atomic Steps (tasks).
   - Invariant: Every step must leave the project compiling cleanly (`dotnet build`, `npm --prefix client run build`) and passing existing tests (`dotnet test`).
   - Include specific verification checks for each step (xUnit tests, client build, manual 2-tab browser checks).

6. Architectural Self-Review
   - Review against .agents/context/project-coding-standards.md.
   - Check against 4-tier severity risks (Blocker, Major, Minor, Nit).

7. Output Spec Document
   - Save the finalized spec under .agents/context/implementations/plans/[feature-name]-spec.md (no date prefix in filename; include date inside content).

## Output

Produce a completed specification based on:

- .agents/skills/gameplay-spec-writing/templates/gameplay-spec-template.md
