# AI Game Development Best Practices for Networking Tic-Tac-Toe

## Goal

Use AI to accelerate delivery while preserving authoritative server security, deterministic turn-based rules, real-time network reliability, and responsive Phaser 4 UI feel.

## Core Principles

1. Keep humans responsible for final game design, rule variations, and UX decisions.
2. Keep the server 100% authoritative over board state, move legality, turn order, and win/draw outcomes.
3. Make AI-generated changes small, reviewable, and covered by deterministic xUnit tests.
4. Preserve strict separation between pure domain rules (`server/Domain/`), network transport (`server/Hubs/`, `client/src/services/`), and visual presentation (`client/src/scenes/`).

## Project-Specific Guardrails

1. **Never trust client state for game rules**: The client only sends player intent (e.g., cell coordinates `(row, col)` or rematch request). The server verifies that the session is active, the caller is the active player, and the target cell is empty before mutating state.
2. **Thread-safe session state**: Multiple SignalR hub requests can arrive concurrently (e.g., simultaneous clicks, disconnect during move processing, simultaneous rematch requests). Synchronize state transitions per session or in the session manager.
3. **Graceful rejection over crashes**: Illegal moves or out-of-order network calls must return a structured rejection/error response to the caller without throwing unhandled exceptions that drop the WebSocket connection or corrupt the room.
4. **Deterministic event ordering**: Ensure state updates (e.g., `MoveMade` followed by `GameOver`, or `PlayerJoined` followed by `GameStarted`) are broadcast in a predictable, documented sequence—or consolidated into atomic state snapshots so clients cannot render out-of-order states.
5. **Clean listener lifecycle**: Always unbind SignalR event handlers when a Phaser scene shuts down so scene restarts or transitions do not trigger duplicate callbacks on destroyed canvas objects.

## High-Value AI Use Cases

### 1. Domain Rule Implementation & Edge-Case Testing

- Implement and refine board representation, win line detection, draw detection, and rematch state transitions in `server/Domain/`.
- Generate comprehensive xUnit `[Theory]` test tables covering all 8 winning lines (3 rows, 3 columns, 2 diagonals), full-board draws, occupied-cell rejections, out-of-turn rejections, and post-game move rejections.

Review checklist:

1. Is the domain logic completely free of SignalR/HTTP dependencies?
2. Are all boundary coordinates (`< 0`, `>= 3`) and invalid states tested?
3. Are state transitions atomic and deterministic?

### 2. Network Protocol & SignalR Synchronization

- Scaffold modular C# DTO records in server/DTOs/ and matching TypeScript interfaces in client/src/types/ (grouping tightly coupled request/response or nested snapshot types into plural files and keeping standalone DTOs in separate files rather than a single monolithic file).
- Implement strongly typed `Hub<IGameClient>` methods and `NetworkService` client wrappers.
- Handle connection lifecycle events (`OnConnectedAsync`, `OnDisconnectedAsync`, reconnects, opponent abandonment).

Review checklist:

1. Do C# DTOs and TypeScript interfaces match in file grouping, property names (accounting for camelCase JSON serialization), and types?
2. Are SignalR group additions/removals properly awaited and cleaned up on disconnect?
3. Does the remaining player receive an immediate, clear notification if their opponent disconnects?

### 3. Phaser 4 Scene & UI Construction

- Build responsive 3x3 grid rendering, hover/click feedback, turn indicators, win-line animations, and rematch prompts in `client/src/scenes/` and `client/src/ui/`.
- Keep input disabled locally when it is not the local player's turn (while still enforcing turn checks on the server).

Review checklist:

1. Are all types strict (zero `any`)?
2. Are network callbacks unregistered on scene `SHUTDOWN` / `DESTROY`?
3. Does the UI clearly distinguish "Waiting for Opponent", "Your Turn (X/O)", "Opponent's Turn", "Victory/Defeat/Draw", and "Opponent Disconnected"?

## Practices for Reliable AI-Assisted Coding

### Scope Before Prompting

1. Define one clear feature slice or subsystem per task.
2. Specify exact target files across `server/`, `client/`, and `tests/`.
3. State constraints explicitly (constructor DI, strict TypeScript, xUnit coverage).

### Validate in Layers

1. **Server Compilation & Unit Tests**: `dotnet build` and `dotnet test` (must pass with 0 errors and 0 warnings).
2. **Client Typecheck & Build**: `npm --prefix client run build` (verifies `tsc` strict typing and Vite bundle).
3. **Architecture Linter**: `powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1`.
4. **Live Two-Client Verification**: Open two browser tabs/windows connected to the server to verify real-time pairing, turn alternation, win/draw screens, rematch flow, and tab-close disconnect handling.

## Common Failure Modes and Mitigations

1. **Failure mode**: Client-server DTO drift (renamed property in C# not updated in TypeScript, or PascalCase vs camelCase mismatch).
   **Mitigation**: Always inspect and update `server/DTOs/` and `client/src/types/` together and verify JSON serialization conventions.
2. **Failure mode**: Race conditions when two players join or click simultaneously.
   **Mitigation**: Protect matchmaking queue operations and session state mutations with appropriate synchronization (locks or thread-safe collections).
3. **Failure mode**: Memory leaks and ghost callbacks in Phaser scenes after rematch or scene transition.
   **Mitigation**: Centralize SignalR connection in a service and unregister scene-specific listeners in Phaser's `SHUTDOWN` event.
4. **Failure mode**: Orphaned sessions when a player closes their browser tab.
   **Mitigation**: Track `Context.ConnectionId` to active session mapping and handle `OnDisconnectedAsync` to notify the peer and dispose of the session.

## When to Ask the User Instead of Guessing

1. Ambiguous matchmaking rules (automatic random queue vs room codes vs both).
2. Rematch behavior (whether X and O swap symbols on rematch, or winner/loser goes first).
3. Disconnect handling policy (immediate forfeit/session close vs grace period for reconnect).
4. Visual theme, resolution, or UI layout preferences not yet specified.
