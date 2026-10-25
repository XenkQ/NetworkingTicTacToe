# Board and Match Rules Domain System Documentation

## Purpose

Use when: modifying, extending, or reviewing pure Tic-Tac-Toe board geometry, symbol placement, win/draw evaluation, turn alternation, player seating, rematch voting, or participant disconnect transitions on the server.

Responsibilities across the repository:
- Owns pure 3x3 grid state (`Board`), zero-based coordinates (`BoardCoordinate`), cell occupancy (`CellState`), and 8-line win detection (`WinningLine`) in server/Domain/Board/.
- Owns the single-match state machine (`GameMatch`), lifecycle statuses (`GameStatus`), rejection classification (`MoveErrorCode`), and playable symbols (`PlayerSymbol`) in server/Domain/Match/.
- Owns deterministic domain outcome records (`JoinMatchResult`, `MoveAttemptResult`, `RematchVoteResult`, `DisconnectResult`) in server/Domain/Results/.
- Aligns serialized domain enum names with frontend literal union types in client/src/types/GameDomainTypes.ts.

Not responsible for:
- Thread synchronization, locking, or `StateVersion` tracking (owned by `GameSessionService` in server/Services/GameSessionService.cs).
- DTO snapshot construction, JSON serialization, or SignalR WebSocket transport (owned by server/Services/GameSessionService.cs and server/Hubs/GameHub.cs).
- Client-side board rendering or hover/click UI feedback (owned by client/src/scenes/BoardScene.ts and client/src/ui/BoardHud.ts).

## Reading Map

- Primary server locations:
  - server/Domain/Board/Board.cs
  - server/Domain/Board/BoardCoordinate.cs
  - server/Domain/Board/CellState.cs
  - server/Domain/Board/WinningLine.cs
  - server/Domain/Match/GameMatch.cs
  - server/Domain/Match/GameStatus.cs
  - server/Domain/Match/MoveErrorCode.cs
  - server/Domain/Match/PlayerSymbol.cs
  - server/Domain/Results/JoinMatchResult.cs
  - server/Domain/Results/MoveAttemptResult.cs
  - server/Domain/Results/RematchVoteResult.cs
  - server/Domain/Results/DisconnectResult.cs
- Primary client locations:
  - client/src/types/GameDomainTypes.ts
- Primary test locations:
  - tests/Domain/Board/BoardTests.cs
  - tests/Domain/Match/GameMatchTests.cs
- Related docs:
  - AGENTS.md
  - .agents/context/shared-coding-standards.md
  - .agents/context/server-coding-standards.md
  - .agents/context/game-systems/matchmaking-and-session-system.md
  - .agents/context/game-systems/signalr-network-protocol-system.md

## Architecture, Protocol, and Data Flow

- Core components & DI registrations:
  - `Board` and `GameMatch` are plain C# classes with no external framework dependencies. They are not registered directly in ASP.NET Core `IServiceCollection`; `GameSessionService` instantiates and owns `GameMatch` internally, which in turn instantiates and owns `Board`.
- Key interfaces & DTO contracts:
  - Domain types are never sent directly over SignalR. `GameSessionService` converts domain state into modular DTOs using `Board.ToJaggedArray()` (`string[3][3]` of `"Empty"`, `"X"`, `"O"`), enum `.ToString()` names, and `WinningLine` coordinates (`First`, `Second`, `Third`).
  - Client union types in client/src/types/GameDomainTypes.ts (`CellValue`, `PlayerSymbolType`, `GameStatusType`, `MoveErrorCodeType`) mirror the exact C# enum identifier strings.
- SignalR RPC & broadcast sequence:
  - None inside server/Domain/. Architecture linter rule `CS-ARCH005` enforces zero `Microsoft.AspNetCore.SignalR` imports in domain files.
- Runtime flow (verify exact branching in server/Domain/Match/GameMatch.cs and server/Domain/Board/Board.cs):
  1. **Player Seating (`GameMatch.TryAddPlayer`)**:
     - Validates `connectionId` with `ArgumentException.ThrowIfNullOrWhiteSpace`.
     - If `connectionId` already matches `PlayerXConnectionId` or `PlayerOConnectionId`, returns an idempotent `JoinMatchResult` with the existing symbol and `DidMatchStart = (Status == GameStatus.InProgress)`.
     - First open seat (`PlayerXConnectionId is null`): assigns `PlayerXConnectionId`, resets the board and match flags, sets `Status = GameStatus.WaitingForPlayers`, and returns `(IsJoined: true, AssignedSymbol: X, DidMatchStart: false)`.
     - Second open seat (`PlayerOConnectionId is null`): assigns `PlayerOConnectionId`, resets the board and match flags, sets `Status = GameStatus.InProgress` and `CurrentTurn = PlayerSymbol.X`, and returns `(IsJoined: true, AssignedSymbol: O, DidMatchStart: true)`.
     - Full match (both seats non-null): returns `(IsJoined: false, AssignedSymbol: null, RejectionReason: "Game is currently full (2/2 players active).", DidMatchStart: false)`.
  2. **Move Validation & Outcome Evaluation (`GameMatch.TryMakeMove`)**:
     - Evaluates guards in strict order and returns `MoveAttemptResult` (`IsValid = false`) without mutating state on failure:
       1. Caller is seated (`MoveErrorCode.NotParticipant`).
       2. `Status == GameStatus.InProgress` (`MoveErrorCode.GameNotInProgress`).
       3. `CurrentTurn == callerSymbol` (`MoveErrorCode.OutOfTurn`).
       4. `Board.IsWithinBounds(row, col)` where `0 <= row < 3` and `0 <= col < 3` (`MoveErrorCode.OutOfBounds`).
       5. `Board.IsCellEmpty(row, col)` (`MoveErrorCode.CellOccupied`).
     - Calls `Board.PlaceSymbol(row, col, callerSymbol.Value)` (increments `OccupiedCount`).
     - Calls `Board.EvaluateWin(callerSymbol.Value)` checking rows `0..2`, columns `0..2`, main diagonal `(0,0)-(1,1)-(2,2)`, and anti-diagonal `(0,2)-(1,1)-(2,0)`.
     - If a `WinningLine` is returned: sets `Status = GameStatus.Won`, `Winner = callerSymbol`, `WinningLine = detectedLine`, and `CurrentTurn = null`.
     - Else if `Board.IsFull()` (`OccupiedCount >= 9`): sets `Status = GameStatus.Draw`, `Winner = null`, `WinningLine = null`, and `CurrentTurn = null`.
     - Otherwise: toggles `CurrentTurn` between `PlayerSymbol.X` and `PlayerSymbol.O` while keeping `Status = GameStatus.InProgress`.
  3. **Rematch Negotiation (`GameMatch.TryAcceptRematch`)**:
     - Rejects non-participants or matches where `Status` is neither `GameStatus.Won` nor `GameStatus.Draw`.
     - Sets `HasPlayerXAcceptedRematch` or `HasPlayerOAcceptedRematch` for the caller.
     - When both flags are `true`: swaps `PlayerXConnectionId` and `PlayerOConnectionId`, calls `Board.Reset()`, clears winner/winning-line/rematch flags, sets `Status = GameStatus.InProgress` and `CurrentTurn = PlayerSymbol.X`, and returns `DidRematchStart = true`.
  4. **Disconnect Cleanup (`GameMatch.HandleDisconnect`)**:
     - Returns `WasParticipant = false` without mutating state when `connectionId` is not seated.
     - Resets board, `CurrentTurn`, `Winner`, `WinningLine`, and rematch vote flags.
     - If one player remains: assigns them to `PlayerXConnectionId`, clears `PlayerOConnectionId = null`, sets `Status = GameStatus.AbortedByDisconnect`, and returns `(true, PlayerXConnectionId, PlayerSymbol.X)`.
     - If no players remain: clears both seats and sets `Status = GameStatus.WaitingForPlayers`.

## Rules and Invariants

- Authoritative validation rules:
  - `GameMatch.TryMakeMove` must pre-validate bounds and occupancy so `Board.PlaceSymbol` never throws `ArgumentOutOfRangeException` or `InvalidOperationException` during normal gameplay RPC handling.
  - Direct callers of `Board.GetCell` or `Board.PlaceSymbol` must supply valid coordinates (`0..2`) and, for `PlaceSymbol`, target an empty cell; violating these preconditions throws exceptions by design.
- Ordering, concurrency, or state transition guarantees:
  - Win detection (`Board.EvaluateWin`) must always execute before full-board draw detection (`Board.IsFull`) so a winning 9th move resolves to `GameStatus.Won` rather than `GameStatus.Draw`.
  - Player `X` always takes the first turn (`CurrentTurn = PlayerSymbol.X`) on initial match start, mutual rematch start (after X/O seat swap), and post-disconnect match start when a new Player `O` joins the promoted waiting Player `X`.
  - `Board` and `GameMatch` are not internally thread-safe; callers must serialize access externally.
- Constraints contributors must preserve:
  - Keep server/Domain/ 100% free of SignalR, ASP.NET Core HTTP, logging, or DTO dependencies (`CS-ARCH005`).
  - Treat enum member names (`CellState`, `PlayerSymbol`, `GameStatus`, `MoveErrorCode`) as wire-contract identifiers because `GameSessionService` serializes them via `.ToString()`. Do not rename enum values without updating client/src/types/GameDomainTypes.ts and UI switch statements.
  - Ask the user before altering player-facing rules (board dimensions, starting symbol, rematch seat-swap policy, or disconnect promotion behavior).

## Extension Points

- Safe extension areas:
  - Adding new read-only query helpers on `Board` or `GameMatch` (e.g., move count inspection or legal-move enumeration for AI/bot opponents).
  - Introducing new `MoveErrorCode` or `GameStatus` variants when coordinated across server/Services/GameSessionService.cs, client/src/types/GameDomainTypes.ts, and client/src/ui/BoardHud.ts.
- Required dependencies and contracts:
  - New domain operations should return immutable positional `record` or `readonly record struct` results under server/Domain/Results/ rather than mutating out-parameters or throwing exceptions for expected player errors.
- Automated xUnit & client testing implications:
  - Every rule change requires deterministic `[Fact]` and `[Theory]` coverage in tests/Domain/Board/BoardTests.cs and tests/Domain/Match/GameMatchTests.cs (all 8 win lines, 9th-move win, full-board draw, out-of-turn/occupied/out-of-bounds/post-game rejections, rematch swap, and disconnect flows).

## Integration Notes

- Upstream dependencies:
  - Only .NET base class library types within `NetworkingTicTacToe.Server.Domain.*`.
- Downstream consumers:
  - `GameSessionService` in server/Services/GameSessionService.cs wraps `GameMatch` inside a lock and projects domain state into DTOs.
  - xUnit test suites in tests/Domain/ instantiate `Board` and `GameMatch` directly.
- Cross-layer coupling risks:
  - Implicit string coupling via `.ToString()` conversion of domain enums in `GameSessionService` to TypeScript string literal unions in client/src/types/GameDomainTypes.ts.

## Known Risks and Open Questions

- Known limitations:
  - `Board` is hardcoded to `BOARD_SIZE = 3` and `WinningLine` models exactly three coordinates (`First`, `Second`, `Third`), so variable grid sizes (e.g., 4x4 or 5x5) would require structural changes to `Board`, `WinningLine`, and client grid layout.
  - Disconnect handling immediately resets the board and transitions to `AbortedByDisconnect` (`PlayerOConnectionId = null`); there is no grace-period seat reservation if a player's WebSocket fully closes and reconnects with a new `ConnectionId`.
- Open design questions:
  - None for the current 3x3 single-match rule set.
- Suggested follow-up tasks:
  - If enum-to-DTO mapping grows, consider explicit switch helper methods in `GameSessionService` instead of `.ToString()` so accidental enum renames fail at compile time or in dedicated contract tests.
