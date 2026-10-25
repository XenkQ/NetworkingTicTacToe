# Project Agent Guide

## Purpose

This is the vendor-neutral entry point for AI agents working in this repository. Use it to map the project, find the authoritative operational files, choose the right workflow, and validate changes without depending on a specific agent vendor.

## Project Map

- Project: `Networking Tic-Tac-Toe`, a 2-player networked turn-based web game with an authoritative backend.
- Client application (Phaser 4 + TypeScript + Vite): `client/`.
- Server application (C# + .NET 10 + ASP.NET Core + SignalR): `server/`.
- Automated test suite (xUnit): `tests/`.
- Agent operational files: `.agents/`.
- User and human-facing documentation: `.user-docs/`.

Generated build and dependency folders such as `node_modules/`, `dist/`, `.vite/`, `bin/`, `obj/`, and `TestResults/` are not source-of-truth project files.
User documentation under `.user-docs/` is written only upon explicit user request and is intended for human readers; agents must not read `.user-docs/` as an operational source of truth.

## Agent Source Order

Read agent guidance in this order:

1. `AGENTS.md` for the project entry point and workflow.
2. `.agents/context/project-coding-standards.md` (index) and the relevant modular coding standards:
   - `.agents/context/shared-coding-standards.md` for cross-stack architectural rules, DTO contract alignment, and documentation conventions.
   - `.agents/context/server-coding-standards.md` for C# (.NET 10), ASP.NET Core, SignalR, and xUnit standards.
   - `.agents/context/client-coding-standards.md` for TypeScript, Phaser 4, Vite, and SignalR client standards.
3. Relevant `.agents/context/adr/*.md` files for architectural decision records.
4. `.agents/context/technology-documentation.md` for official documentation links.
5. `.agents/context/ai-game-dev-best-practices.md` for networked gameplay and review guardrails.
6. Relevant `.agents/context/game-systems/*-system.md` files when the task touches a documented game system.
7. Relevant `.agents/skills/*/SKILL.md` files when the task matches a skill trigger.

Use `.agents/context/` as the current documentation location for agent-facing project guidance.
Use `.agents/context/adr/` for architecture decision records (ADRs).
Use `.agents/context/game-systems/` for game-system documentation.
Store brainstorming summaries under `.agents/context/brainstorming-summaries/`.
Store implementation plans under `.agents/context/implementations/plans/` and implementation summaries under `.agents/context/implementations/summaries/`.
Store all temporary multi-agent exchange files, prompt roadmaps, and scratch data exclusively under `.agents/context/tmp/`.
Do not read `.user-docs/` as an operational source of truth for agent reasoning; operational truth derives strictly from the codebase and `.agents/`.

## Technology Baseline

- **Client**: Phaser 4 + TypeScript bundled with Vite under `client/`.
- **Server**: C# (.NET 10) + ASP.NET Core hosting SignalR hubs and static client assets under `server/`.
- **Networking**: SignalR over WebSockets with JSON serialization (`@microsoft/signalr` on the client).
- **Dependency Injection**: ASP.NET Core built-in `IServiceCollection` and constructor injection on the server.
- **Testing**: xUnit on .NET 10 under `tests/` for deterministic domain, board, move, and session testing.

When behavior depends on Phaser 4, TypeScript, Vite, ASP.NET Core, SignalR, or xUnit APIs, consult official documentation through `.agents/context/technology-documentation.md` before relying on memory.

## Core Architecture Rules

- **Authoritative Server**: The server owns the canonical game state, turn order, move legality, win/draw evaluation, and session lifecycle. The client renders state and dispatches player intent.
- **Explicit Dependencies**: Register server services in `Program.cs` (or dedicated service registration extensions) via ASP.NET Core DI (`IServiceCollection`) and inject them through constructors.
- **Separation of Concerns**:
  - Keep pure game rules and board state in `server/Domain/` free of SignalR or HTTP dependencies so they are directly testable via xUnit.
  - Keep SignalR transport logic in `server/Hubs/` and session coordination in `server/Services/`.
  - Keep client SignalR connection management in `client/src/services/` decoupled from Phaser rendering scenes in `client/src/scenes/`.
- **Contract Alignment**: Keep C# DTOs/records in `server/DTOs/` strictly synchronized with TypeScript interfaces in `client/src/types/` (or `client/src/models/`).
- **No Static Mutable State**: Do not introduce uncontrolled static mutable state; manage multi-request or active-session state through thread-safe registered services.
- **Clean Lifecycle Management**: Unregister SignalR listeners and Phaser input/scene events when scenes shut down or connections close.

## Coding Standards

Follow `.agents/context/project-coding-standards.md` and its modular companions (`.agents/context/shared-coding-standards.md`, `.agents/context/server-coding-standards.md`, and `.agents/context/client-coding-standards.md`) for detailed dual-stack rules. The highest-impact rules are:

- **Server (C# / .NET 10)**:
  - Public members, properties, methods, types, records, and events use `PascalCase`.
  - Private fields use `_camelCase` (prefer `readonly` where assigned only in the constructor).
  - Constants use `UPPER_SNAKE_CASE`.
  - Prefix interfaces with `I` and colocate narrow single-implementation interfaces above their implementation.
  - Standard LINQ is permitted and encouraged for expressive domain queries and test assertions.
  - Methods use standard `{}` block syntax (expression-bodied properties are allowed).
- **Client (TypeScript / Phaser 4)**:
  - Enable and respect `strict: true` in `tsconfig.json`; never use explicit `any`.
  - Variables, functions, methods, and properties use `camelCase`.
  - Classes, interfaces, types, and enums use `PascalCase`.
  - Constants use `UPPER_SNAKE_CASE`.
- **Shared Rules**:
  - Treat compiler and linter warnings as errors during development.
  - Keep legacy cleanup incremental and scoped to touched code.
  - **English Language Invariant**: All repository files, code identifiers, comments, XML doc comments, JSDoc comments, and agent operational files must be authored strictly in English. Multilingual communication is permitted exclusively in user-facing chat interactions.

## Gameplay & Networking Guardrails

- Keep board evaluation, move validation, turn switching, win/draw detection, and rematch transitions 100% deterministic and covered by xUnit tests.
- Reject illegal moves (out of turn, occupied cell, out of bounds, inactive game) gracefully on the server without crashing the hub or corrupting session state.
- Preserve message and event ordering across matchmaking, move broadcast, game over, rematch negotiation, and player disconnect flows.
- Handle abrupt client disconnections deterministically, notifying the remaining player and cleaning up orphaned sessions.
- Ask the user before changing player-facing game rules, network protocol contracts, matchmaking behavior, or visual layout conventions.

## Common System Areas

- Server Entry & DI Wiring: `server/Program.cs`.
- Domain & Game Rules: `server/Domain/`.
- SignalR Hubs & RPC Contracts: `server/Hubs/`, `server/DTOs/`.
- Session & Matchmaking Services: `server/Services/`.
- Client Entry & Configuration: `client/src/main.ts`, `client/vite.config.ts`, `client/tsconfig.json`.
- Client Scenes & UI: `client/src/scenes/`, `client/src/ui/`.
- Client Networking & State Models: `client/src/services/`, `client/src/models/` (or `client/src/types/`).
- Server Unit & Integration Tests: `tests/`.

Verify exact behavior in code before making behavioral claims; this guide is a map, not a replacement for source inspection.

## Agent Skills

Use `.agents/skills/` as reusable workflows:

- `agent-docs-review`: update agent-facing documentation so it is concise, current, and implementation-grounded.
- `architecture-review`: review DI correctness, client-server ownership boundaries, network protocol safety, and architecture drift.
- `batch-codebase-review`: partition and orchestrate project-wide architecture review and coding standards preservation via subagents or generated prompt roadmaps.
- `batch-system-docs-update`: refresh existing system documentation through size-aware batches of one to three systems, delegated workers, and cross-batch verification.
- `create-user-doc`: create or update human-facing, user-oriented project documentation in `.user-docs/` upon explicit user request.
- `document-system`: create or update technical documentation for a specific gameplay or networking system under `.agents/context/game-systems/`.
- `game-brainstorm`: explore gameplay ideas, UX flows, network features, and architecture trade-offs before creating code or specs.
- `gameplay-spec-writing`: write or review staff-engineer level technical specifications and implementation plans with hard Open Questions gates.
- `phaser-core`: set up, structure, and debug Phaser 4 scenes, canvas scaling, lifecycle cleanup, and reactive client rendering in strict TypeScript.
- `pre-commit-gate`: run a comprehensive pre-commit verification gate (server build, xUnit tests, client build/typecheck, standards audit, git consistency, lifecycle docs).
- `preserve-coding-standards`: audit and safely fix coding-standard drift in a pointed C# or TypeScript scope.
- `root-cause`: systematically investigate and diagnose server, SignalR, or Phaser client bugs and race conditions in read-only mode.

When a request matches a skill trigger, read the relevant `SKILL.md` before editing.

## Agent Descriptors

All 12 repository-local skills have an optional Codex descriptor at `.agents/skills/<skill-name>/agents/openai.yaml`. Each descriptor supplies `display_name`, `short_description`, and a `default_prompt` naming its skill. Invocation policy retains the default; descriptors do not grant action authority.

Treat these YAML files as optional UI/default-prompt metadata for compatible tools. The portable workflow source remains each skill's `SKILL.md`.

## Implementation Lifecycle & In-Repo Storage Invariant

For every non-trivial task, feature, or refactor, follow this two-phase repository documentation lifecycle:

1. **Planning Phase (Before Code Changes)**:
   - Create the implementation plan directly in the repository at `.agents/context/implementations/plans/[feature-name]-plan.md` (or `[feature-name]-spec.md`).
   - Use `.agents/context/implementations/templates/plan-template.md` as reference.
   - Stop and confirm requirements / open questions with the user before editing code.
2. **Execution Phase**:
   - Implement the approved changes in code adhering to `.agents/context/project-coding-standards.md`.
3. **Summary Phase (Upon Completion)**:
   - Create the implementation summary directly in the repository at `.agents/context/implementations/summaries/[feature-name]-summary.md` (or `[feature-name].md`).
   - Use `.agents/context/implementations/templates/summary-template.md` as reference.
   - Document changes made, automated/manual validation performed, and any follow-up checks.

**Strict Negative Constraints**:
- **NEVER** save implementation plans or summaries exclusively to external IDE directories (e.g. `brain/`, `AppData`, `/tmp`, or user profile folders).
- If an IDE environment (such as Antigravity) generates an artifact for UI preview, the authoritative in-repo markdown file under `.agents/context/implementations/` must ALWAYS be created/updated first.

## Multi-Agent & Temporary Files Invariant

- **Designated Workspace Location**: All temporary files, scratch scripts, inter-agent handoff state documents (such as `batch_review_handoff.md`), agent prompt roadmaps (`agent_prompts_roadmap.md`), and raw intermediate data produced during multi-agent orchestration or task execution MUST be stored strictly under `.agents/context/tmp/` (or the IDE session scratch directory).
- **Strict Negative Constraint**: Agents are **STRICTLY FORBIDDEN** from creating temporary files, scratch scripts, handoff files, intermediate notes, `.tmp`, `.txt`, `.json`, or temporary `.md` files in the repository root (`/`), `client/`, `server/`, `tests/`, or any other workspace directory outside `.agents/context/tmp/`.
- **Git Hygiene**: `.agents/context/tmp/` is ignored in `.gitignore` (preserving `.gitkeep`), keeping git status clean during parallel and sequential multi-agent operations.

## Work Workflow

1. Identify the requested scope and the smallest relevant files.
2. Read this guide, `GEMINI.md` (if running under Gemini/Antigravity), the relevant `.agents/context/*` file, and any triggered skill.
   For game systems, read the relevant `.agents/context/game-systems/*-system.md` file.
3. Create or update the implementation plan under `.agents/context/implementations/plans/` when planning non-trivial changes.
4. Inspect source files before changing behavior or documenting concrete implementation details.
5. Keep edits scoped; do not mix broad cleanup with domain logic, networking, or UI changes.
6. Prefer existing project patterns over new abstractions.
7. Protect user work in a dirty worktree. Do not revert changes you did not make.
8. Validate with targeted compile and test commands when possible.
9. Create the implementation summary under `.agents/context/implementations/summaries/`.
10. Summarize files changed, validation performed, and any remaining manual browser/network checks.

## Validation

For C# server and test changes, run build and unit tests:

```powershell
dotnet build
dotnet test
```

For TypeScript / Phaser client changes, run typechecking and production build inside `client/`:

```powershell
npm --prefix client run build
```

For architecture and standards linting:

```powershell
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

For documentation-only changes, review links and paths for accuracy. A live browser check with two connected client tabs/windows is still required for visual layout, SignalR real-time synchronization, turn transitions, and disconnect/rematch UX.

## Documentation Maintenance

- Keep agent operational guidance under `.agents/`.
- Keep this root `AGENTS.md` as a short entry point, not a full replacement for `.agents/context/*`.
- Keep game-system documentation in `.agents/context/game-systems/` (created and updated organically as systems are built).
- Keep brainstorming session summaries in `.agents/context/brainstorming-summaries/`.
- Keep implementation plans in `.agents/context/implementations/plans/` and summaries in `.agents/context/implementations/summaries/`.
- Keep all temporary agent runtime data, scratch files, and multi-agent handoff files in `.agents/context/tmp/`.
- Use `.agents/context/implementations/templates/` for plan and summary structure.
- Keep human-facing project documentation in `.user-docs/`, creating or editing files there only upon explicit user request.
- Do not prefix filenames for implementation plans, summaries, or brainstorm briefs with dates; specify the date inside the file content instead.
- Write file and directory paths in agent documentation relative to the project root as plain text, without markdown links or backticks (e.g. `- server/Hubs/GameHub.cs`).
- When adding a new specialist domain, add or update the matching skill or agent file under `.agents/`.
- Keep skill descriptions explicit with `Use when:` trigger language.
- Update cross-references when docs move or filenames change.
- Maintain all agent operational files, plans, summaries, and skill templates in English.
