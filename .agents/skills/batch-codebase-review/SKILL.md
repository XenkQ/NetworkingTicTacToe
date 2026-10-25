---
name: batch-codebase-review
description: "Use when: orchestrating or partitioning an explicitly requested project-wide or multi-system Networking Tic-Tac-Toe architecture and coding-standards review. Triggers: batch codebase review, partition codebase review, multi-agent code review, parallel codebase audit. Supports audit-only reviews and separately authorized scoped fixes."
---

# Batch Codebase Review

Coordinate architecture-review and preserve-coding-standards across the requested C# (`server/`, `tests/`) and TypeScript (`client/src/`) scope. Select audit or fix mode before partitioning. A single file review or compile request does not require this broad workflow.

## Required Sources

- AGENTS.md
- .agents/context/project-coding-standards.md
- .agents/skills/architecture-review/SKILL.md
- .agents/skills/preserve-coding-standards/SKILL.md
- .agents/context/ai-game-dev-best-practices.md
- Relevant ADRs and game-system documents for each batch.

Use .agents/skills/pre-commit-gate/SKILL.md only when a comprehensive gate is requested. Consult official sources through .agents/context/technology-documentation.md before relying on framework behavior.

## Scope and Authority

- **Audit mode:** inspect code, report findings and proposed fixes, and run relevant read-only verification. Do not edit code, even for a seemingly safe standards fix.
- **Fix mode:** apply only explicitly authorized corrections within the named scope. Reuse existing authorization; an audit finding does not authorize implementation. Preserve behavior, DTO contracts, DI lifetimes, and event order.
- Inspect git status and user changes before work. Preserve unrelated modifications. Record existing diagnostics rather than adopting them as repair work.
- Before non-trivial code edits, create or update .agents/context/implementations/plans/[task-name]-plan.md using the canonical plan template and resolve consequential requirements with the user. Reuse approved plans and decisions. An audit-only run also records its durable scope plan; no code-edit gate is needed for its report.
- Write the completion report under .agents/context/implementations/summaries/[task-name]-summary.md using the canonical summary template. Temporary state never replaces the durable plan or summary.

## Inventory and Partitioning

Inventory the requested scope with `rg --files` and inspect file sizes, responsibilities, DI edges, and shared contracts. Record actual file counts rather than assuming historical batch sizes. Aim for cohesive batches of about 10-30 files, splitting large areas by layer/ownership and avoiding artificial splits that hide coupled contracts.

Possible domain groups, subject to the current inventory:

- Server Domain & Rules: `server/Domain/` and `server/Constants/`.
- Server Services, Hubs & DTOs: `server/Services/`, `server/Hubs/`, `server/DTOs/`, `server/Program.cs`.
- Server Automated Tests: `tests/`.
- Client Networking & Models: `client/src/services/`, `client/src/types/` (or `client/src/models/`).
- Client Phaser Scenes & UI: `client/src/scenes/`, `client/src/ui/`, `client/src/main.ts`.

Every scoped file must have one review owner. In fix mode assign one writer per file; reserve shared DTO contracts, `Program.cs`, and cross-batch edits for the coordinator. Workers report required shared changes instead of editing them. Do not redispatch a scope until its previous writer has released ownership.

## Tracking and Execution

Keep scratch state under .agents/context/tmp/:

- .agents/context/tmp/batch_review_plan.md: inventory, action mode, ownership, and durable-plan reference.
- .agents/context/tmp/batch_review_handoff.md: batch status, findings, diagnostics, coverage, and next actions.
- .agents/context/tmp/agent_prompts_roadmap.md: optional prompts for external sessions.

Use the bundled plan and handoff templates. Track PENDING, IN_PROGRESS, REVIEW, DONE, and BLOCKED. A reviewed batch can be DONE with unresolved reported findings; DONE describes accepted coverage, not a clean build or fixed code. Record a separate verification verdict.

Choose the branch supported by the environment:

1. **Delegated execution:** use available collaboration/subagent capabilities rather than requiring a vendor-specific API name. Give each worker the selected mode, exact writable files (none in audit mode), readable integration files, required guidance, and report contract. Workers must not broaden scope or delegate further. The coordinator accepts diffs and reconciles shared findings after ownership is released.
2. **Sequential execution:** process the same batches and update checkpoints after each accepted review or authorized fix.
3. **Prompt roadmap:** generate self-contained prompts with scope, authority, ownership, evidence requirements, and compile coordination. This delivers a roadmap, not an executed codebase audit; record actual execution as pending.

## Compilation and Correction Boundaries

The coordinator owns shared compilation and test execution. Serialize `dotnet build` / `dotnet test` and `npm --prefix client run build` in a shared checkout:

```powershell
dotnet build
dotnet test
npm --prefix client run build
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

In audit mode, report diagnostics without fixes. In fix mode, correct introduced diagnostics only within authorized ownership, then re-run the affected check. Report unrelated baseline warnings/errors and unavailable tools with evidence. Do not extend repairs or loop indefinitely without progress; finish independent batches and record the precise unresolved dependency. Required zero-warning verification is FAIL or PENDING until demonstrated.

On resumption, read the durable plan and handoff, verify current diffs and owner state, and continue unfinished batches. Recheck accepted coverage only when its source or dependencies changed.

## Acceptance and Output

Accept each batch after reviewing its coverage, findings, scope compliance, and any authorized diff. Reconcile cross-batch DI contracts, DTO alignment, lifecycle/event ordering, and duplicate findings. Distinguish review coverage, repair completion, and verification status.

Write the durable summary with actual batches, findings by severity, files changed or reviewed without changes, deferred fixes, baseline failures, executed checks, pending manual checks, and completion limits. Do not claim code ready when required checks failed or remain pending. Do not commit unless requested.

## Templates

- .agents/skills/batch-codebase-review/templates/batch-review-plan-template.md
- .agents/skills/batch-codebase-review/templates/batch-review-handoff-template.md
- .agents/context/implementations/templates/plan-template.md
- .agents/context/implementations/templates/summary-template.md
