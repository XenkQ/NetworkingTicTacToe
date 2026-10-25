---
name: pre-commit-gate
description: "Use when: the user requests a comprehensive Networking Tic-Tac-Toe pre-commit or pre-merge verification gate covering server compilation, xUnit tests, client TypeScript/Vite build, DI & DTO contract alignment, coding standards, git consistency, and lifecycle documentation. Triggers: pre-commit gate, check and commit, full validation gate, pre-merge check."
---

# Pre-Commit Gate

Verify the changed scope across the full stack with six gates. Verification does not itself authorize repairs, commits, or unrelated cleanup.

## Required Sources

- AGENTS.md
- .agents/context/project-coding-standards.md
- .agents/context/ai-game-dev-best-practices.md
- Relevant ADRs and game-system docs under .agents/context/.

Consult official sources through .agents/context/technology-documentation.md before relying on framework behavior.

## Scope and Authority

- A requested full pre-commit gate requires all six gates. A narrow compile or test request runs only the targeted command unless the user requests the full gate.
- Inspect `git status` and `git diff`, including authorized untracked additions. Preserve unrelated user work; a dirty worktree is not itself a failure.
- Verification-only mode reports findings without edits. Existing explicit repair authority permits scoped compatible fixes after the durable plan/requirements gate for non-trivial code edits.
- Fix introduced diagnostics within authorized scope. Report unrelated baseline diagnostics, missing tools/projects, and blocked checks. Do not broaden repairs or run an unproductive fix loop.
- Use `PASS`, `FAIL`, `PENDING`, or `N/A` with evidence for every gate. `N/A` requires a scope-based reason (e.g., documentation-only change); unavailable checks are `PENDING`. Overall `PASS` requires all applicable checks to pass.

## Six Gates

### 1. Server Compilation & xUnit Tests (`server/`, `tests/`)

For C# server or test changes, execute:

```powershell
dotnet build
dotnet test
```

Require exit code 0, zero errors, zero warnings, and 100% passing xUnit tests. Report actual diagnostics and test counts. For client-only or documentation-only changes, record as `N/A` with reason (unless shared contracts were touched).

### 2. Client TypeScript & Vite Build (`client/`)

For TypeScript / Phaser client changes, execute:

```powershell
npm --prefix client run build
```

Require exit code 0, zero TypeScript compiler errors (`strict: true`), and a clean Vite production bundle. For server-only or documentation-only changes, record as `N/A` with reason.

### 3. Client-Server DTO & DI Contract Safety

- Verify that any touched DTOs in `server/DTOs/` and `client/src/types/` match in property names, types, and JSON serialization conventions.
- Verify that SignalR Hub RPC method names and parameter lists in `server/Hubs/` match client invocations/listeners in `client/src/services/`.
- Verify that all injected C# services are properly registered in `server/Program.cs` with appropriate lifetimes (`Singleton` vs `Scoped`) and thread-safe state handling.

### 4. Coding Standards & Architecture Linter

Execute the automated architecture and standards linter:

```powershell
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

Also inspect the changed scope against `.agents/context/project-coding-standards.md`:
- C#: `_camelCase` private fields, `UPPER_SNAKE_CASE` constants, `PascalCase` public members, `I` interface prefix, `{}` block syntax for methods, pure `server/Domain/` free of SignalR dependencies.
- TypeScript: zero `any` types, networking isolated in `client/src/services/`, Phaser scene listeners cleaned up on `SHUTDOWN` / `DESTROY`.
- Shared: English Language Invariant strictly maintained.

### 5. Git & Workspace Hygiene

- Verify that no temporary, scratch, or handoff files were left in the repository root, `client/`, `server/`, or `tests/` (all scratch files must live under `.agents/context/tmp/`).
- Verify that `node_modules/`, `dist/`, `bin/`, `obj/`, and `TestResults/` remain untracked via `.gitignore`.
- Preserve pre-existing user edits and untracked files; do not stage or revert unrelated work.

### 6. Implementation Lifecycle Documentation

For non-trivial changes, verify that the durable implementation plan exists under `.agents/context/implementations/plans/`, consequential requirements were resolved, and the implementation summary exists under `.agents/context/implementations/summaries/`. Temporary state and external IDE artifacts do not replace them.

## Workflow and Output

Scope the diff, run applicable checks, inspect contracts/DI/standards, and report results using `.agents/skills/pre-commit-gate/templates/pre-commit-gate-checklist.md`. If repairs are authorized, apply only scoped corrections and repeat checks affected by those edits. A full gate with failures or pending checks is not passed. Report manual browser checks and remaining blockers explicitly; commit only when the request authorizes it.
