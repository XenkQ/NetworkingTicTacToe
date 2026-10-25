# ADR 0003: In-Memory Single-Session Concurrency and Seat Lifecycle Policy

- **Status:** Accepted
- **Date:** 2026-10-25
- **Use when:** Modifying matchmaking, player slot assignment, 3rd-client room-full handling, rematch symbol swapping, or disconnect recovery behavior.

## Context

For the MVP scope, two LAN players need frictionless browser entry into a shared match without account registration, lobby codes, or external database setup. At the same time, SignalR invokes hub methods concurrently across thread-pool threads—meaning simultaneous `JoinGame`, `MakeMove`, `RequestRematch`, and `OnDisconnectedAsync` calls can race against shared match state, and additional browser tabs (3rd+ clients) or mid-match tab closures must behave predictably.

## Decision

1. **In-Memory Singleton Session Synchronized via `Lock`:**
   - Register `IGameSessionService` (`GameSessionService`) as a single in-memory singleton in ASP.NET Core DI (server/Program.cs), encapsulating one active `GameMatch` instance without external persistence (no SQLite/Redis).
   - Serialize all state reads, mutations, `StateVersion` increments, and immutable DTO snapshot projections inside a dedicated `.NET 10` `System.Threading.Lock` (`_syncRoot`), releasing the lock before `GameHub` awaits network I/O broadcasts.
2. **Deterministic Two-Seat Lifecycle & 3rd-Client `Room Busy` Fallback:**
   - First joiner claims Player `X` (`WaitingForPlayers`); second joiner claims Player `O` and immediately starts the match (`InProgress`, `CurrentTurn = X`).
   - A 3rd+ client calling `JoinGame` while both seats are occupied receives `IsJoined = false` along with the current read-only `GameStateDto` snapshot, entering `Spectator (Room Busy)` mode with a manual `Retry Join` action to refresh the board or claim a seat once one opens.
3. **Mutual Rematch with `X <-> O` Seat Swap:**
   - After `Won` or `Draw`, both seated participants must vote via `RequestRematch`. Upon the second vote (`2/2`), the server swaps `PlayerXConnectionId` and `PlayerOConnectionId`, resets the board, sets `CurrentTurn = X` (giving the previous Player `O` the first move), and keeps 3rd+ clients locked out across the transition.
4. **Immediate Disconnect Abort & Promotion to Waiting Player `X`:**
   - Disconnecting a non-participant/spectator is a no-op (`WasParticipant = false`).
   - Disconnecting an active participant (during play or post-game rematch voting) immediately aborts/resets the match, promotes any remaining participant to `PlayerXConnectionId` (`AbortedByDisconnect`, `AssignedSymbol = X`), and leaves the `O` slot open so the next connecting or `Retry Join` client starts a fresh match immediately.

## Alternatives Considered

- **Multi-Room Lobby / Room Codes in MVP:** Rejected for initial baseline to keep LAN onboarding zero-click (open URL -> play immediately) while preserving a clean `IGameSessionService` boundary that can later map `roomId -> GameMatch`.
- **Disconnect Grace Period / Session Token Re-Seating:** Rejected. Holding a dead seat open on LAN blocks the remaining human player when someone simply closes a browser tab; immediate promotion to waiting Player `X` plus automatic reconnect re-joining (`connectAndJoinAsync` -> claiming open `O` seat) provides instant recovery without stale timers.
- **Live Push Streaming to 3rd+ Spectators:** Deferred as an optional post-MVP extension; returning point-in-time snapshots via `JoinGame` / `Retry Join` satisfies the room-busy requirement without tracking spectator SignalR groups.

## Consequences and Guardrails

- **Positive:**
  - Zero external infrastructure dependencies, zero lock contention across network I/O, and 100% deterministic concurrency behavior verified in tests/Services/GameSessionServiceTests.cs.
- **Trade-offs & Invariants:**
  - Server process restart clears in-memory match state by design.
  - Never introduce `static` mutable state (`CS-ARCH001`) or hold `_syncRoot` across `await Clients.*` calls in `GameHub`.

## Authoritative Files and Related Docs

- Session & domain lifecycle: server/Services/GameSessionService.cs, server/Domain/Match/GameMatch.cs, server/Program.cs
- Client spectator & reconnect handling: client/src/services/GameNetworkService.ts, client/src/scenes/BoardScene.ts
- Automated concurrency & unit tests: tests/Services/GameSessionServiceTests.cs
- Related system doc: .agents/context/game-systems/matchmaking-and-session-system.md
