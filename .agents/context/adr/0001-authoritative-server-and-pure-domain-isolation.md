# ADR 0001: Authoritative Server State and Pure Domain Isolation

- **Status:** Accepted
- **Date:** 2026-10-25
- **Use when:** Modifying game rules, move validation, win/draw evaluation, session state ownership, or client move submission flows.

## Context

Networking Tic-Tac-Toe is a 2-player real-time turn-based game played across separate browsers over a Local Area Network (LAN). Two structural risks arise immediately in networked browser games:

1. Trusting client-side state or optimistic board mutation introduces split-brain desynchronization, illegal move exploits, and rollback complexity when two players click nearly simultaneously or experience transient latency.
2. Embedding board geometry, turn alternation, and win/draw evaluation directly inside SignalR hub methods or ASP.NET Core services couples deterministic game rules to WebSocket transport, forcing tests to mock hub contexts or spin up network hosts.

## Decision

1. **100% Server Authority (Zero Optimistic Client Mutation):**
   - The ASP.NET Core backend is the sole owner of canonical match state, player seating (`Context.ConnectionId` to `PlayerSymbol`), turn order, move legality, win/draw resolution, rematch voting, and disconnect transitions.
   - The Phaser client dispatches intent only (for example, `(row, col)` coordinates in `MakeMoveRequestDto` without any client-claimed symbol) and renders board symbols exclusively after receiving server-confirmed state snapshots.
2. **Framework-Free Pure C# Domain Layer:**
   - All board representation (`Board`, `BoardCoordinate`, `CellState`, `WinningLine`), match state transitions (`GameMatch`, `GameStatus`, `MoveErrorCode`, `PlayerSymbol`), and immutable domain outcomes (`JoinMatchResult`, `MoveAttemptResult`, `RematchVoteResult`, `DisconnectResult`) live in server/Domain/ with zero references to `Microsoft.AspNetCore.*` or SignalR.
   - Architecture linter rule `CS-ARCH005` in .agents/scripts/check-architecture.ps1 statically blocks `Microsoft.AspNetCore.SignalR` imports anywhere under server/Domain/.
   - Transport adaptation happens strictly above the domain: `GameSessionService` coordinates locking and DTO projection, while `GameHub` handles SignalR RPC dispatch.

## Alternatives Considered

- **Optimistic Client Placement with Rollback on Rejection:** Rejected. Over LAN and local WebSockets, round-trip latency is negligible for a turn-based grid game, whereas rolling back speculative tweens, winning lines, and turn banners on `OutOfTurn` or `CellOccupied` races adds brittle client complexity with minimal UX benefit.
- **Game Logic Directly Inside `GameHub` or Transport Services:** Rejected. Mixing WebSocket broadcast concerns with 3x3 rule evaluation harms single-responsibility clarity and prevents fast, isolated xUnit matrix testing of all 8 winning lines and 9th-move win-before-draw edge cases.

## Consequences and Guardrails

- **Positive:**
  - Deterministic, sub-millisecond xUnit test execution across board boundaries, illegal moves, rematch swaps, and disconnect promotions in tests/Domain/ without network fixtures.
  - Future rule extensions (such as a server-side AI opponent using the Strategy pattern or multi-room match instances) can reuse `Board` and `GameMatch` unchanged.
- **Trade-offs & Invariants:**
  - Clients must gate local input (`_isMovePending`) while awaiting server confirmation and render structured rejection feedback (`OnMoveRejected`) instead of mutating cells locally.
  - Domain types in server/Domain/ must never be serialized directly over the wire; map them explicitly to modular DTOs in server/DTOs/.

## Authoritative Files and Related Docs

- Server domain: server/Domain/Board/Board.cs, server/Domain/Match/GameMatch.cs, server/Domain/Results/
- Session & transport boundary: server/Services/GameSessionService.cs, server/Hubs/GameHub.cs
- Automated enforcement & tests: .agents/scripts/check-architecture.ps1, tests/Domain/Board/BoardTests.cs, tests/Domain/Match/GameMatchTests.cs
- Related system doc: .agents/context/game-systems/board-and-rules-system.md
