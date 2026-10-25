# Shared Coding Standards & Architectural Rules

## Purpose

This document defines cross-stack architectural principles, language invariants, contract synchronization rules, and documentation standards shared across both the C# (.NET 10) server/test projects and the TypeScript (Phaser 4) client application.

Use this document alongside:
- .agents/context/server-coding-standards.md for C#, .NET 10, ASP.NET Core, SignalR, and xUnit rules.
- .agents/context/client-coding-standards.md for TypeScript, Phaser 4, Vite, and SignalR client rules.
- .agents/context/project-coding-standards.md for the top-level standards index and routing guide.

## Scope

Apply these rules to:

1. All files across server/, tests/, client/, and .agents/.
2. New code and documentation added to existing files.
3. Modified sections in refactors.

When a file contains legacy style, prefer incremental migration in touched areas instead of broad formatting-only rewrites.

---

## 1) English Language Invariant

- All repository files, source code identifiers (classes, structs, records, interfaces, methods, properties, fields, parameters, local variables, enum values, namespaces, modules), comments, XML docstrings, JSDoc comments, log messages, templates, and agent documentation must be authored strictly in English.
- Multilingual communication is supported exclusively in direct user-facing chat, never in repository artifacts.

---

## 2) Authoritative Server & Ownership Boundaries

- **Authoritative Server**: The server owns the canonical game state, board representation, turn order, move legality, win/draw evaluation, and session lifecycle.
- **Client Intent & Presentation**: The client renders server-provided state and dispatches player intent (such as cell coordinates or rematch requests). Never trust client state for game rules, turn validation, or win detection.
- **Strict Layer Separation**:
  - Pure game rules and board state live in server/Domain/ free of SignalR or HTTP dependencies.
  - SignalR transport lives in server/Hubs/ and session coordination lives in server/Services/.
  - Client SignalR connection management lives in client/src/services/ decoupled from Phaser rendering scenes in client/src/scenes/.

---

## 3) Client-Server Contract Alignment

- Keep C# DTO records in server/DTOs/ strictly synchronized with TypeScript interfaces in client/src/types/ (or client/src/models/).
- Account for ASP.NET Core SignalR's default camelCase JSON serialization:
  - C# positional record properties use `PascalCase` (e.g., `public sealed record MakeMoveRequest(int Row, int Col);`).
  - TypeScript DTO interface properties use `camelCase` matching the serialized JSON payload (e.g., `row: number; col: number;`).
- Ensure property names, data types, enum representations, and nullability match across both stacks whenever modifying network payloads or Hub RPC signatures.

---

## 4) File Organization & Interface Colocation

### One Primary Runtime Type Per File
- Keep each file focused on one primary class, service, scene, or component.
- Supporting small enums, DTO records, or narrow interface contracts may live in the same file when tightly related to that primary type.

### Interface Colocation
- In C# server code, define a narrow interface and its primary implementation in the same file (with the interface placed above the class) whenever the interface is owned and consumed primarily for dependency injection of that single implementation.
- Shared contracts consumed across multiple unrelated modules or layers may live in dedicated contract or DTO files.

---

## 5) Fail-Fast Validation & Error Handling

- Validate inputs at system boundaries (Hub RPC entry points, service methods, and domain operations).
- Guard constructor dependencies against null where appropriate.
- Reject illegal gameplay actions (out-of-turn moves, occupied cells, out-of-bounds coordinates, inactive sessions) deterministically without throwing unhandled exceptions that crash the SignalR connection or corrupt active rooms.

---

## 6) Zero Warnings Policy

- Treat compiler warnings (`dotnet build`, `tsc`) and architecture linter warnings (.agents/scripts/check-architecture.ps1) as errors during development.
- Resolve all warnings and linter violations before completing a task.

---

## 7) Markdown & Agent Documentation Rules

- AI-facing project documentation under .agents/context/ uses `kebab-case` filenames.
- Game-system documentation belongs under .agents/context/game-systems/ and uses `kebab-case` filenames ending in `-system.md`.
- Implementation plans under .agents/context/implementations/plans/ and summaries under .agents/context/implementations/summaries/ must use `kebab-case` filenames without any date prefix (e.g., `signalr-matchmaking-plan.md`). Specify the date inside the file content.
- Plans and summaries must be stored directly in the repository and never solely in external IDE/runtime directories.
- All temporary agent runtime data, scratch scripts, and multi-agent handoff files must be stored strictly under .agents/context/tmp/.
- File and directory paths in agent documentation must be written relative to the project root as plain text without markdown links and without backticks (e.g. - server/Domain/Board.cs or - client/src/scenes/BoardScene.ts).
- Reserved operational filenames keep their established uppercase names (`AGENTS.md`, `GEMINI.md`, `README.md`, `SKILL.md`).
- When renaming documentation or skills, update all relative references in .agents/context/, .agents/skills/, and root agent entry-point files.

---

## 8) Cross-Stack Agent Execution Checklist

Before finalizing any change across the repository:

1. Server C# code compiles cleanly with zero warnings (`dotnet build`) and all xUnit tests pass (`dotnet test`).
2. Client TypeScript code typechecks and builds cleanly (`npm --prefix client run build`).
3. Architecture linter passes with zero errors and zero warnings (`powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1`).
4. Client-server DTO contracts match in property names, JSON casing, types, and nullability.
5. Authoritative server validation is preserved (no client-trusted win or move state).
6. Implementation plan (.agents/context/implementations/plans/) and summary (.agents/context/implementations/summaries/) are created and tracked in the repository for non-trivial tasks.
7. All newly written or touched code identifiers, comments, logs, templates, and markdown files are strictly in English.
