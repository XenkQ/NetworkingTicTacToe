---
name: preserve-coding-standards
description: "Use when: auditing or applying authorized fixes within a provided Networking Tic-Tac-Toe scope for drift from .agents/context/project-coding-standards.md. Triggers: preserve coding standards, coding standards cleanup, style drift, naming/order cleanup, fix standards violations, align scope with project standards."
---

# Preserve Coding Standards

Inspect the named C# (`server/`, `tests/`) or TypeScript (`client/src/`) scope against the canonical standards. An audit-only request produces findings; a cleanup/fix request permits compatible incremental corrections within that scope.

## Required Sources

- AGENTS.md
- .agents/context/project-coding-standards.md
- .agents/context/ai-game-dev-best-practices.md

Consult official sources through .agents/context/technology-documentation.md before relying on framework behavior.

## Scope and Lifecycle

- Use the named files, classes, folders, or systems. Infer only the narrowest clear active scope; clarify consequential ambiguity while inspecting independent evidence.
- Exclude generated directories (`bin/`, `obj/`, `node_modules/`, `dist/`) and vendor code. Preserve unrelated user changes and avoid broad legacy rewrites.
- Before non-trivial code changes, create or update the durable implementation plan and resolve requirements under AGENTS.md. Reuse existing authorization and approved decisions. Write the durable implementation summary upon completion.

## Checklist

- **C# Server & Tests (`server/`, `tests/`)**:
  - Member order: constants, private readonly fields, constructors, public properties, public methods, private helpers.
  - Private fields: `_camelCase`. Constants: `UPPER_SNAKE_CASE`.
  - Public members and types: `PascalCase`. Narrow single-implementation interfaces colocated above their implementation and prefixed with `I`.
  - Methods use `{}` block syntax (no expression-bodied `=>` methods; expression-bodied properties are permitted).
  - No static mutable singleton state (`public static Instance`); use ASP.NET Core constructor DI.
  - Pure domain classes in `server/Domain/` have no SignalR transport dependencies.
- **TypeScript Client (`client/src/`)**:
  - Strict typing: zero explicit `any` types, zero `@ts-ignore` / `@ts-nocheck` suppressions.
  - Naming: `camelCase` for variables/methods/properties, `PascalCase` for classes/interfaces/types/enums, `UPPER_SNAKE_CASE` for constants.
  - Networking isolated in `client/src/services/` (no `HubConnectionBuilder` inside Phaser scenes).
  - Scene event listeners properly unsubscribed on Phaser scene `SHUTDOWN` / `DESTROY`.
- **Shared**:
  - English Language Invariant strictly followed across all identifiers, comments, logs, and docs.
  - Preserve client-server DTO contract alignment and deterministic turn/event ordering.

## Fix Classification

For every candidate inspect references, DTO serialization contracts, event consumers, and initialization effects before classifying it.

- **Compatible within authorized cleanup:** reorder declarations when initialization behavior is unchanged; rename private fields/locals after updating all references in the file; replace `=>` method bodies with `{}` block bodies; replace `any` with explicit existing types; remove unused imports.
- **Requires complete consumer analysis and existing implementation authority:** relocate constants, extract interfaces, or encapsulate public fields after accounting for all usages across `server/`, `tests/`, and `client/`. If authority or compatibility is uncertain, propose the change rather than applying it.
- **Public or network contract change:** SignalR Hub method renames, DTO property renames (`server/DTOs/` <-> `client/src/types/`), or DI service lifetime changes affect cross-layer consumers. Obtain missing authority and update both client and server contracts together before editing.

## Workflow and Verification

1. Inventory with `rg --files`, read the scope and its consumers, and record user changes and available baseline diagnostics.
2. Classify candidates and either report them in audit mode or apply compatible authorized fixes after the lifecycle gate.
3. Verify implemented changes:

```powershell
dotnet build
dotnet test
npm --prefix client run build
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

4. Require exit code 0 with zero errors and warnings across applicable checks. In authorized fix mode, repair diagnostics introduced by these edits within the owned scope and re-run the check. Do not repair unrelated baseline failures or expand scope.
5. Inspect the diff for DTO compatibility, consumers, and behavioral safety. Report unavailable checks as pending.

## Output

Report audited scope, changed files, violations fixed or proposed, deliberately preserved/deferred items, baseline versus introduced diagnostics, actual verification results, and durable artifacts. Do not claim a clean gate without evidence.
