# Matchmaking and Session Management System Documentation

## Purpose

Use when: modifying, extending, or reviewing server-side player pairing, active match session lifecycle, thread synchronization, monotonic `StateVersion` generation, or domain-to-DTO snapshot creation.

Responsibilities across the repository:
- Owns `IGameSessionService`, `GameSessionService`, and service-layer result records (`JoinSessionResult`, `MoveSessionResult`, `RematchSessionResult`, `DisconnectSessionResult`) in server/Services/GameSessionService.cs.
- Registers `IGameSessionService` as an application-wide singleton in server/Program.cs.
- Serializes concurrent SignalR hub requests against the active `GameMatch` using `System.Threading.Lock`.
- Tracks monotonic `_stateVersion` across authoritative state mutations and constructs immutable `GameStateDto` snapshots.

Not responsible for:
- Pure board geometry, win/draw rules, or seat-swap domain logic (delegated to `GameMatch` and `Board` in server/Domain/).
- SignalR connection groups, WebSocket push dispatch, or caller RPC transport (owned by `GameHub` in server/Hubs/GameHub.cs).
- Multi-room matchmaking queues or room-code lobbies (not implemented in the current architecture; see Known Risks and Open Questions).

## Reading Map

- Primary server locations:
  - server/Services/GameSessionService.cs
  - server/Program.cs
- Primary client locations:
  - client/src/services/GameNetworkService.ts
  - client/src/scenes/BoardScene.ts
  - client/src/ui/BoardHud.ts
- Primary test locations:
  - tests/Services/GameSessionServiceTests.cs
- Related docs:
  - AGENTS.md
  - .agents/context/shared-coding-standards.md
  - .agents/context/server-coding-standards.md
  - .agents/context/game-systems/board-and-rules-system.md
  - .agents/context/game-systems/signalr-network-protocol-system.md

## Architecture, Protocol, and Data Flow

- Core components & DI registrations:
  - server/Program.cs registers `builder.Services.AddSingleton<IGameSessionService, GameSessionService>()`.
  - `IGameSessionService` is colocated above `GameSessionService` in server/Services/GameSessionService.cs per project interface-colocation standards.
  - `GameSessionService` holds `private readonly Lock _syncRoot = new()`, `private readonly GameMatch _match = new()`, and `private long _stateVersion`.
- Key interfaces & DTO contracts:
  - `IGameSessionService` exposes `JoinSession`, `MakeMove`, `RequestRematch`, `HandleDisconnect`, `GetCurrentGameState`, and `GetParticipantConnectionIds`.
  - Produces DTOs from server/DTOs/ (`GameStateDto`, `WinningLineDto`, `BoardCellCoordinateDto`, `JoinGameResponseDto`, `MoveResultDto`, `PlayerAssignmentDto`, `RematchResponseDto`), mirrored in client/src/types/.
- SignalR RPC & broadcast sequence:
  - `GameSessionService` never invokes SignalR APIs directly. Instead, every mutating method returns both the DTO payload and a snapshot of `ParticipantConnectionIds` (or targeted `NewPlayerXConnectionId` / `NewPlayerOConnectionId` / `RemainingPlayerConnectionId`) so `GameHub` can dispatch notifications outside the lock.
- Runtime flow (verify exact lock scope and version increments in server/Services/GameSessionService.cs):
  1. **Single-Room Seating (`JoinSession`)**:
     - Acquires `_syncRoot`, checks `_match.IsParticipant(connectionId)`, and calls `_match.TryAddPlayer(connectionId)`.
     - Increments `_stateVersion` only when `joinResult.IsJoined && !wasAlreadyParticipant`.
     - First player gets `AssignedSymbol = "X"`, `DidMatchStart = false`, `Status = "WaitingForPlayers"`.
     - Second player gets `AssignedSymbol = "O"`, `DidMatchStart = true`, `Status = "InProgress"`, `CurrentTurn = "X"`, and returns both participant IDs so `GameHub` can broadcast match start.
     - Third concurrent connection when 2/2 seats are occupied receives `IsJoined = false`, `AssignedSymbol = null`, `RejectionReason = "Game is currently full (2/2 players active)."`, and the current `GameStateDto` snapshot without incrementing `_stateVersion`.
  2. **Move Processing (`MakeMove`)**:
     - Acquires `_syncRoot` and delegates to `_match.TryMakeMove(connectionId, row, col)`.
     - On invalid move: leaves `_stateVersion` unchanged and returns `MoveSessionResult(IsAccepted: false)` with error code/message and current `GameStateDto`.
     - On valid move: increments `_stateVersion`, computes contextual `StatusMessage` (`Won`, `Draw`, or `InProgress`), and returns `MoveSessionResult(IsAccepted: true)` with `ParticipantConnectionIds`.
  3. **Rematch Coordination (`RequestRematch`)**:
     - Acquires `_syncRoot`, checks whether caller had already voted (`hadAlreadyVoted`), and calls `_match.TryAcceptRematch(connectionId)`.
     - On rejection: leaves `_stateVersion` unchanged and returns `DidVoteChange = false`.
     - On accepted vote: sets `didVoteChange = !hadAlreadyVoted || rematchResult.DidRematchStart` and increments `_stateVersion` only if `didVoteChange` is `true` (duplicate clicks by the same player are idempotent and do not bump `_stateVersion`).
     - When `DidRematchStart` is `true`: builds individual `PlayerAssignmentDto` payloads (`"X"` and `"O"`) for the swapped connection IDs sharing the same post-reset `GameStateDto` snapshot.
  4. **Disconnect Handling (`HandleDisconnect`)**:
     - Acquires `_syncRoot` and calls `_match.HandleDisconnect(connectionId)`.
     - Non-participant disconnects return `WasParticipant = false` without bumping `_stateVersion`.
     - Participant disconnects increment `_stateVersion`. If a peer remains, returns `RemainingPlayerConnectionId` along with `PlayerAssignmentDto("X", gameState)` in `AbortedByDisconnect` status so the peer becomes waiting Player `X` ready for the next joiner.

## Rules and Invariants

- Authoritative validation rules:
  - All public service methods accepting `connectionId` guard with `ArgumentException.ThrowIfNullOrWhiteSpace(connectionId)` before acquiring `_syncRoot`.
- Ordering, concurrency, or state transition guarantees:
  - Every read and mutation of `_match` and `_stateVersion` executes inside `lock (_syncRoot)`.
  - Snapshots returned from `lock (_syncRoot)` must be deeply immutable or freshly allocated copies (`Board.ToJaggedArray()` allocates a new jagged array; `GetParticipantConnectionIdsCore()` allocates a new `List<string>`), ensuring `GameHub` never reads mutable state after releasing the lock.
  - `_stateVersion` is strictly monotonic: it starts at `0`, increments by `1` exclusively on real state changes (new join, valid move, non-duplicate rematch vote, participant disconnect), and never decreases during process lifetime.
- Constraints contributors must preserve:
  - Do not introduce static mutable fields or singleton `Instance` properties (`CS-ARCH001`); keep state scoped to the DI-managed `GameSessionService` instance.
  - Never perform `await` or network I/O inside `lock (_syncRoot)`.

## Extension Points

- Safe extension areas:
  - Evolving from a single `GameMatch` room to multi-room FIFO matchmaking or room-code lobbies behind `IGameSessionService` while keeping `GameHub` thin.
  - Adding reconnect grace-period tokens (mapping a persistent client session token to a player seat before `HandleDisconnect` aborts the match).
- Required dependencies and contracts:
  - If multi-room sessions are introduced, maintain a `ConnectionId -> RoomId` index alongside per-room locks (or synchronized room entries) so `MakeMove`, `RequestRematch`, and `HandleDisconnect` resolve the caller's room in O(1) without global lock contention across unrelated matches.
- Automated xUnit & client testing implications:
  - Update or extend tests/Services/GameSessionServiceTests.cs, including parallel stress tests (`Concurrency_SimultaneousJoinRequests_OnlyTwoPlayersSeated`, `Concurrency_SimultaneousMoves_SerializedThreadSafely`) and `StateVersion` monotonicity tests (`StateVersion_IncrementsMonotonically_AcrossStateMutations`, `RequestRematch_DuplicateVoteFromSamePlayer_DoesNotIncrementStateVersion`).

## Integration Notes

- Upstream dependencies:
  - `GameMatch`, `Board`, domain enums, and result records in server/Domain/.
  - DTO positional records in server/DTOs/.
- Downstream consumers:
  - `GameHub` in server/Hubs/GameHub.cs consumes `IGameSessionService` via constructor injection.
  - `GameNetworkService` (client/src/services/GameNetworkService.ts) and `BoardScene` (client/src/scenes/BoardScene.ts) rely on `GameStateDto.stateVersion` monotonicity to discard stale/out-of-order snapshots.
- Cross-layer coupling risks:
  - `StatusMessage` strings generated in `GameSessionService` are included in `GameStateDto.statusMessage`, though `BoardHud` currently derives primary banner text from `status`, `currentTurn`, `winner`, and `isJoined` while `BoardScene` displays `rejectionReason` / `errorMessage` in feedback banners. Keep both structured fields and human-readable messages accurate.

## Known Risks and Open Questions

- Known limitations:
  - Single global match capacity: only 2 players can play across the entire server process at one time; any 3rd connection enters an unsubscribed `"Spectator (Room Busy)"` state and must manually click `Retry Join` after a slot frees up.
  - In-memory state only: restarting the ASP.NET Core server resets `_match` and `_stateVersion` back to `0`. If a client stays open across a server restart without disconnecting first, verify reconnect behavior (`onreconnecting` / `onclose` resets client `_latestGameState` and `_latestAssignmentVersion` to `0`).
- Open design questions:
  - Should future matchmaking automatically queue a 3rd+ player into a new room instance, or place them into a live-updating spectator group for the single room? Confirm with the user before changing matchmaking behavior.
- Suggested follow-up tasks:
  - Perform live 3-tab browser verification of the room-busy rejection and `Retry Join` transition after one active tab closes (currently unverified in this documentation pass).
