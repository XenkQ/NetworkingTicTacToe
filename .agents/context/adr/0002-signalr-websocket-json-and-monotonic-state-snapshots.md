# ADR 0002: SignalR WebSocket JSON Transport and Monotonic State Snapshots

- **Status:** Accepted
- **Date:** 2026-10-25
- **Use when:** Extending Hub RPC methods, `IGameClient` push events, DTO contracts, reconnection logic, or client state synchronization.

## Context

The project specification requires bidirectional TCP-based real-time communication exchanging JSON documents between the C# server and browser-based Phaser clients. However:

1. Browser security sandboxes prohibit web scripts from opening raw OS-level TCP sockets directly.
2. Concurrent asynchronous SignalR broadcasts (for example, rapid move broadcasts, mutual rematch `OnPlayerAssignmentChanged` + `OnGameStateUpdated` notifications, or automatic reconnections) can arrive at clients out of order or overlap with direct RPC return payloads.

## Decision

1. **SignalR over WebSockets with JSON Protocol:**
   - Use ASP.NET Core SignalR (`Hub<IGameClient>` mapped at `/hubs/game`) paired with `@microsoft/signalr` on the client using default camelCase JSON serialization over persistent WebSocket (TCP) connections.
   - Synchronize modular C# positional DTO records in server/DTOs/ (`PascalCase` properties) 1:1 with strict TypeScript DTO interfaces in client/src/types/ (`camelCase` properties).
2. **Full Atomic State Snapshots over Delta Events:**
   - Every state-changing lifecycle event broadcasts a complete, self-contained `GameStateDto` snapshot (3x3 board array, `CurrentTurn`, `Status`, `Winner`, `WinningLine`, rematch vote flags, and `StateVersion`) rather than incremental delta messages (such as `CellUpdated` or `TurnSwitched`).
3. **Monotonic `StateVersion` Ordering Guard:**
   - `GameSessionService` maintains a strictly monotonic `long _stateVersion` incremented inside `lock (_syncRoot)` only on genuine state transitions (skipping increments on idempotent re-joins or duplicate rematch votes).
   - `GameNetworkService` and `BoardScene` discard stale `GameStateDto` payloads whose `stateVersion` is older than the highest version already applied, while tracking symbol reassignments via an independent `_latestAssignmentVersion` so a newer board snapshot (`v + 1`) never suppresses an earlier assignment event (`v`).
   - Cached client versions reset to `0` on `onreconnecting` and `onclose` so reconnections after a server restart cleanly accept new server epochs starting at version `1`.

## Alternatives Considered

- **Raw Browser WebSockets with Custom JSON Framing:** Rejected. Re-implementing RPC correlation IDs, connection heartbeats, automatic reconnect backoff (`[0, 1000, 2000, 5000, 10000]` ms), and strongly typed client dispatch adds boilerplate already solved cleanly by SignalR.
- **HTTP Polling / REST Endpoints:** Rejected. Introduces polling latency and complicates immediate server-initiated notifications on opponent move or abrupt tab-close disconnect.
- **Granular Delta Events (`MovePlaced`, `GameEnded`):** Rejected. A 3x3 board snapshot is tiny (~200-300 bytes of JSON); full snapshots make state rendering idempotent, prevent partial-update drift, and allow reconnecting or spectator clients to hydrate from a single `GameStateDto`.

## Consequences and Guardrails

- **Positive:**
  - Browser-native TCP/WebSocket compliance with human-readable JSON payloads and end-to-end compile-time typing across C# (`Hub<IGameClient>`) and TypeScript.
  - Immunity to out-of-order async snapshot delivery and duplicate RPC/broadcast rendering.
- **Trade-offs & Invariants:**
  - Any modification to C# records in server/DTOs/ requires an atomic matching update in client/src/types/ respecting camelCase JSON serialization.
  - Read-only queries (`GetGameState`), rejected moves, idempotent re-joins, and duplicate rematch votes must never increment `StateVersion`.

## Authoritative Files and Related Docs

- Server contracts & hub: server/DTOs/, server/Hubs/GameHub.cs, server/Services/GameSessionService.cs
- Client contracts & transport: client/src/types/, client/src/services/GameNetworkService.ts
- Automated tests: tests/Hubs/GameHubTests.cs, tests/Services/GameSessionServiceTests.cs
- Related system doc: .agents/context/game-systems/signalr-network-protocol-system.md
