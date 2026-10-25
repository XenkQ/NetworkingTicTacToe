---
name: root-cause
description: "Use when: investigating and diagnosing bugs, runtime exceptions, failing xUnit tests, SignalR desynchronization, race conditions, disconnect bugs, or Phaser client rendering/input issues without making premature code changes. Triggers: root cause, investigate bug, find bug, why is this broken, exception, trace defect, bug investigation."
---

# Root Cause Skill

Use this skill for read-only systematic bug investigation across the C# (.NET 10 / SignalR) server, xUnit tests, and TypeScript (Phaser 4) client. It traces defects to their exact source, categorizes failure domains, and defines the minimal change set needed for a safe fix without modifying codebase files during the investigation.

## Hard Gate (Read-Only)

This skill is strictly read-only.
- Do not edit C# files (`server/`, `tests/`), TypeScript files (`client/`), or configuration files during diagnosis.
- Do not make speculative fixes while investigating.
- The output is a clear, actionable Root Cause Report that guides the subsequent fix.

## Required Sources

Ground the investigation in:

- AGENTS.md
- .agents/context/project-coding-standards.md
- .agents/context/ai-game-dev-best-practices.md
- Relevant game system docs under .agents/context/game-systems/

## Full-Stack Defect Categories

Classify the defect into one or more categories:

1. **Domain Rules & Board Logic (`server/Domain/`)**: Win/draw evaluation errors, turn alternation bugs, coordinate validation flaws, or dirty state on rematch reset.
2. **SignalR Protocol & DTO Serialization (`server/Hubs/`, `server/DTOs/`, `client/src/services/`)**: Mismatched hub method names, JSON property casing/type mismatches, or dropped events.
3. **Session Concurrency & Disconnect Lifecycle (`server/Services/`)**: Race conditions when two players join or act simultaneously, uncleaned sessions on disconnect, or incorrect `ConnectionId` mapping.
4. **ASP.NET Core DI & Configuration (`server/Program.cs`)**: Missing service registrations, wrong service lifetime (`Scoped` vs `Singleton` causing lost in-memory sessions), or CORS/WebSocket routing issues.
5. **Phaser 4 Scene Lifecycle & UI State (`client/src/scenes/`, `client/src/ui/`)**: Duplicate event listeners after scene restart (missing `SHUTDOWN` unsubscription), input remaining enabled during opponent's turn, or stale visual objects.

## Workflow

1. Ingest Symptom & Reproduction Steps
   - Analyze error message, stack trace, failing xUnit test output, browser console log, or multiplayer anomaly described by the user.
   - Note affected layers (`server/`, `client/`, `tests/`).

2. Trace Code Path & Execution Flow
   - Trace the request from client input (`Phaser Scene` -> `NetworkService.invoke`) through the server (`GameHub` -> `SessionService` -> `Domain`) and back via SignalR broadcast (`Clients.Group` -> `NetworkService.on` -> `Phaser Scene`).
   - Identify where state diverges, exceptions occur, or messages are lost.

3. Audit Contracts, Lifetimes & Cleanup
   - Check DI lifetimes in `server/Program.cs` (e.g. an in-memory session manager registered as `Transient` or `Scoped` instead of `Singleton` will lose state between hub calls).
   - Check C# DTOs vs TypeScript interfaces.
   - Check Phaser scene `SHUTDOWN` cleanup for `NetworkService` listeners.

4. Formulate & Confirm Root Cause
   - Identify the primary flaw causing the defect with concrete file and line evidence.
   - Explain why the bug occurs and under what exact conditions.

5. Define Minimal Change Set
   - Specify the exact files, methods, and lines to modify.
   - Ensure the proposed fix is minimal and preserves architectural boundaries.
   - Identify regression risks and propose verification steps (`dotnet test`, `npm --prefix client run build`, manual 2-tab browser test). Separate investigation checks actually performed from proposed post-fix checks.

## Output

Produce a structured report based on:

- .agents/skills/root-cause/templates/root-cause-report-template.md
