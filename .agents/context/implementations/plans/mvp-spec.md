# Specification: Networking Tic-Tac-Toe MVP

**Date:** 2026-10-25  
**Author:** Antigravity Agent  
**Target Systems:** server/Domain/, server/DTOs/, server/Services/, server/Hubs/, server/Program.cs, client/src/types/, client/src/services/, client/src/scenes/, client/src/ui/, tests/  

---

## 1. Overview & Player Experience

- **Summary:** Complete technical specification and phased implementation plan for the Minimum Viable Product (MVP) of Networking Tic-Tac-Toe. The system enables two players on separate computers within the same Local Area Network (LAN) to play a real-time, turn-based 3x3 Tic-Tac-Toe match in modern web browsers powered by Phaser 4 (TypeScript + Vite) on the client and an authoritative C# (.NET 10 + ASP.NET Core + SignalR) server.
- **Architectural Intent:**
  - **Authoritative Server State:** A single active match state is maintained in server memory via a thread-safe singleton service. The server exclusively governs player slot assignment, turn order, move legality, win/draw evaluation, rematch negotiation, and disconnect recovery.
  - **Object-Oriented Domain Purity:** All board rules, coordinate validation, win/draw detection, and match state transitions reside in pure C# classes under server/Domain/ with zero dependencies on SignalR or HTTP abstractions, enabling fast, deterministic xUnit verification under tests/.
  - **Decoupled Client Presentation:** The browser client separates SignalR WebSocket transport (client/src/services/) from Phaser 4 scene rendering and input handling (client/src/scenes/ and client/src/ui/). Network operations are asynchronous and never block the UI thread.
  - **Self-Contained LAN Hosting:** The ASP.NET Core server hosts both the SignalR hub endpoint (`/hubs/game`) and the built Phaser client static assets so LAN players can join simply by opening the server HTTP address in their browser.
- **Player-Facing Goals:**
  - **Joining & Matchmaking:** The first connected client joins as Player `X` and sees `"Waiting for opponent..."`. The second connected client joins as Player `O`, immediately starting the match on an empty 3x3 board with Player `X` taking the first turn. Any third client connecting while two players occupy the match receives a `"Game is currently full (2/2 players active)"` message and cannot interfere with the match.
  - **Making Moves & Feedback:** Clicking an empty cell during the local player's turn sends a move request to the server. Valid moves update both players' boards in real time with smooth symbol placement animations. Illegal moves (out of turn, occupied cell, out of bounds, inactive match, or non-participant) are rejected without altering the board.
  - **End-of-Match & Rematch (With Symbol Swap):** When the server detects a 3-in-a-row win (row, column, or diagonal) or a 9-move draw, further moves are blocked and both clients display the outcome (including a visual highlight of the winning line). Both participants can click `"Accept Rematch"`. Once both accept, players swap symbols (`X` becomes `O`, `O` becomes `X`), the board resets to empty, and the new Player `X` makes the first move while keeping third clients locked out.
  - **Disconnect Recovery:** If either participant disconnects during an active match or during post-game rematch negotiation, the server terminates the match, notifies the remaining player (`"Opponent disconnected. Waiting for a new opponent..."`), resets the board, and retains the remaining player as waiting Player `X`. As soon as a returning or new second player connects, they are assigned `O` and a fresh match starts immediately.
- **In-Scope vs. Out-of-Scope:**
  - **In-Scope:** Single concurrent 2-player match in server memory, 3x3 board rules, third-client busy rejection, server move validation, row/column/diagonal win and draw detection, disconnect interruption and automatic waiting recovery, mutual rematch with symbol swap, LAN static asset hosting from ASP.NET Core, Phaser 4 UI displaying board, assigned symbol, turn, result, and connection status, plus xUnit automated tests.
  - **Out-of-Scope:** Multi-room lobbies, persistent database storage, user authentication, chat systems, AI opponents, spectator live-streaming, or turn timers.

---

## 2. Open Questions & Resolved Decisions

### Resolved Decisions
- **Single In-Memory Match Instance:** The server manages one concurrent match for two players in memory and reports the game as busy/full to any third client attempting to join while two players are present (Source: User MVP specification).
- **Initial Turn Rule:** Upon both players joining, the server creates an empty 3x3 board and always grants the first move to Player `X` (Source: User MVP specification).
- **Rematch Symbol Swap (Q1 Resolved):** When both players accept a rematch after a finished game, their symbols swap (`X <-> O`) so the previous Player `O` becomes Player `X` and takes the first turn on the new empty board (Source: User decision A1).
- **Disconnect Recovery Flow (Q2 Resolved):** When one participant disconnects, the server aborts the match, displays a disconnect notification to the remaining player, and automatically keeps the remaining player in the session as waiting Player `X`; when a returning or new player joins as `O`, a new match starts immediately (Source: User decision A2).
- **English UI Strings (Q3 Resolved):** All player-facing UI messages, status banners, and button labels in Phaser are authored in English, consistent with the repository-wide English Language Invariant (Source: User decision A3).
- **Validation & Testing Scope:** All move validation and win/draw/rematch rules are verified via deterministic xUnit tests; multi-client LAN sync, UI feedback, and disconnect behavior are verified via integration tests and live two-client browser demonstration (Source: User MVP specification).

### Open Questions (Consequential Unknowns Only)
- None. All consequential gameplay, protocol, and UX decisions are resolved.

---

## 3. Domain Model & Network Contracts

### Server Domain Models (server/Domain/)

All domain types use file-scoped namespaces, contain zero transport dependencies, enforce deterministic state transitions, and are partitioned into descriptive domain subdirectories:

1. **Board Subdomain (server/Domain/Board/ — namespace `NetworkingTicTacToe.Server.Domain.Board`):**
   - server/Domain/Board/CellState.cs:
     ```csharp
     public enum CellState
     {
         Empty = 0,
         X = 1,
         O = 2
     }
     ```
   - server/Domain/Board/BoardCoordinate.cs:
     ```csharp
     public readonly record struct BoardCoordinate(int Row, int Col);
     ```
   - server/Domain/Board/WinningLine.cs:
     ```csharp
     public sealed record WinningLine(
         BoardCoordinate First,
         BoardCoordinate Second,
         BoardCoordinate Third);
     ```
   - server/Domain/Board/Board.cs:
     - Constant `public const int BOARD_SIZE = 3;` and `public const int TOTAL_CELLS = 9;`.
     - Encapsulates private `readonly CellState[,] _cells = new CellState[BOARD_SIZE, BOARD_SIZE];` and `private int _occupiedCount;`.
     - Public methods (using standard `{}` block syntax):
       - `public bool IsWithinBounds(int row, int col)`
       - `public CellState GetCell(int row, int col)`
       - `public bool IsCellEmpty(int row, int col)`
       - `public void PlaceSymbol(int row, int col, PlayerSymbol symbol)`
       - `public void Reset()`
       - `public WinningLine? EvaluateWin(PlayerSymbol symbol)` — deterministically checks all 3 rows, 3 columns, and 2 diagonals.
       - `public bool IsFull()` — returns `_occupiedCount >= TOTAL_CELLS`.
       - `public string[][] ToJaggedArray()` — exports 3x3 strings (`"Empty"`, `"X"`, `"O"`) for DTO serialization.

2. **Match Subdomain (server/Domain/Match/ — namespace `NetworkingTicTacToe.Server.Domain.Match`):**
   - server/Domain/Match/PlayerSymbol.cs:
     ```csharp
     public enum PlayerSymbol
     {
         X = 1,
         O = 2
     }
     ```
   - server/Domain/Match/GameStatus.cs:
     ```csharp
     public enum GameStatus
     {
         WaitingForPlayers = 0,
         InProgress = 1,
         Won = 2,
         Draw = 3,
         AbortedByDisconnect = 4
     }
     ```
   - server/Domain/Match/MoveErrorCode.cs:
     ```csharp
     public enum MoveErrorCode
     {
         None = 0,
         GameNotInProgress = 1,
         NotParticipant = 2,
         OutOfTurn = 3,
         OutOfBounds = 4,
         CellOccupied = 5
     }
     ```
   - server/Domain/Match/GameMatch.cs:
     - Owns the lifecycle of the single match session:
       - `public Board Board { get; }`
       - `public string? PlayerXConnectionId { get; private set; }`
       - `public string? PlayerOConnectionId { get; private set; }`
       - `public PlayerSymbol? CurrentTurn { get; private set; }`
       - `public GameStatus Status { get; private set; }`
       - `public PlayerSymbol? Winner { get; private set; }`
       - `public WinningLine? WinningLine { get; private set; }`
       - `public bool HasPlayerXAcceptedRematch { get; private set; }`
       - `public bool HasPlayerOAcceptedRematch { get; private set; }`

3. **Results Subdomain (server/Domain/Results/ — namespace `NetworkingTicTacToe.Server.Domain.Results`):**
   - server/Domain/Results/MoveAttemptResult.cs:
     ```csharp
     public sealed record MoveAttemptResult(
         bool IsValid,
         MoveErrorCode ErrorCode,
         string? ErrorMessage,
         PlayerSymbol? MovedSymbol,
         BoardCoordinate? Coordinate,
         GameStatus ResultingStatus,
         PlayerSymbol? Winner,
         WinningLine? WinningLine);
     ```
   - server/Domain/Results/JoinMatchResult.cs:
     ```csharp
     public sealed record JoinMatchResult(
         bool IsJoined,
         PlayerSymbol? AssignedSymbol,
         string? RejectionReason,
         bool DidMatchStart);
     ```
   - server/Domain/Results/RematchVoteResult.cs:
     ```csharp
     public sealed record RematchVoteResult(
         bool IsAccepted,
         string? ErrorMessage,
         bool DidRematchStart);
     ```
   - server/Domain/Results/DisconnectResult.cs:
     ```csharp
     public sealed record DisconnectResult(
         bool WasParticipant,
         string? RemainingPlayerConnectionId,
         PlayerSymbol? RemainingPlayerNewSymbol);
     ```
   - Key domain operations:
     - `public JoinMatchResult TryAddPlayer(string connectionId)`:
       - If caller is already `PlayerXConnectionId` or `PlayerOConnectionId`, returns their existing symbol idempotently.
       - If `PlayerXConnectionId is null`, assigns caller to `X`, sets `Status = GameStatus.WaitingForPlayers`, clears board, returns `DidMatchStart = false`.
       - If `PlayerOConnectionId is null`, assigns caller to `O`, resets board, sets `Status = GameStatus.InProgress`, sets `CurrentTurn = PlayerSymbol.X`, clears rematch flags, and returns `DidMatchStart = true`.
       - Otherwise (both slots occupied), returns `IsJoined = false`, `AssignedSymbol = null`, `RejectionReason = "Game is currently full (2/2 players active)."`.
     - `public MoveAttemptResult TryMakeMove(string connectionId, int row, int col)`:
       - Validates in strict order:
         1. Participant check (`connectionId` matches `PlayerXConnectionId` or `PlayerOConnectionId`) -> otherwise `MoveErrorCode.NotParticipant`.
         2. Active match status (`Status == GameStatus.InProgress`) -> otherwise `MoveErrorCode.GameNotInProgress`.
         3. Turn ownership (`callerSymbol == CurrentTurn`) -> otherwise `MoveErrorCode.OutOfTurn`.
         4. Coordinate bounds (`Board.IsWithinBounds(row, col)`) -> otherwise `MoveErrorCode.OutOfBounds`.
         5. Cell availability (`Board.IsCellEmpty(row, col)`) -> otherwise `MoveErrorCode.CellOccupied`.
       - Mutates board via `Board.PlaceSymbol(row, col, callerSymbol)`.
       - Evaluates win via `Board.EvaluateWin(callerSymbol)`:
         - If win detected: sets `Status = GameStatus.Won`, `Winner = callerSymbol`, `WinningLine = line`, `CurrentTurn = null`.
         - Else if `Board.IsFull()`: sets `Status = GameStatus.Draw`, `Winner = null`, `WinningLine = null`, `CurrentTurn = null`.
         - Else: switches `CurrentTurn` (`X -> O` or `O -> X`).
     - `public RematchVoteResult TryAcceptRematch(string connectionId)`:
       - Verifies caller is a participant and `Status is GameStatus.Won or GameStatus.Draw`.
       - Marks `HasPlayerXAcceptedRematch = true` or `HasPlayerOAcceptedRematch = true`.
       - When both flags are `true`: swaps `(PlayerXConnectionId, PlayerOConnectionId) = (PlayerOConnectionId, PlayerXConnectionId)`, resets `Board.Reset()`, clears `Winner`, `WinningLine`, and rematch flags, sets `Status = GameStatus.InProgress`, and sets `CurrentTurn = PlayerSymbol.X`.
     - `public DisconnectResult HandleDisconnect(string connectionId)`:
       - If `connectionId` is neither `PlayerXConnectionId` nor `PlayerOConnectionId`, returns `WasParticipant = false`.
       - Removes the disconnecting player, resets the board and rematch flags, and if the other player is still connected:
         - Assigns the remaining connected player to `PlayerXConnectionId` and sets `PlayerOConnectionId = null`.
         - Sets `Status = GameStatus.AbortedByDisconnect`, `CurrentTurn = null`, `Winner = null`, `WinningLine = null`.
         - Returns `WasParticipant = true`, `RemainingPlayerConnectionId`, and `RemainingPlayerNewSymbol = PlayerSymbol.X`.
       - If no player remains connected, resets `Status = GameStatus.WaitingForPlayers` with both player slots `null`.

---

### SignalR DTO Contracts (server/DTOs/ & client/src/types/)

All C# DTOs are immutable positional `record` types in server/DTOs/GameContracts.cs (serialized to `camelCase` JSON by ASP.NET Core SignalR) and mirrored by strict TypeScript interfaces in client/src/types/GameContracts.ts:

| C# Record (server/DTOs/GameContracts.cs) | TypeScript Interface (client/src/types/GameContracts.ts) | Purpose |
|---|---|---|
| `BoardCellCoordinateDto(int Row, int Col)` | `BoardCellCoordinateDto { readonly row: number; readonly col: number; }` | Coordinates for moves and winning line highlights |
| `WinningLineDto(BoardCellCoordinateDto[] Cells)` | `WinningLineDto { readonly cells: readonly BoardCellCoordinateDto[]; }` | 3 winning coordinates for rendering win line |
| `GameStateDto(string[][] Cells, string Status, string? CurrentTurn, string? Winner, WinningLineDto? WinningLine, bool IsPlayerXConnected, bool IsPlayerOConnected, bool HasPlayerXAcceptedRematch, bool HasPlayerOAcceptedRematch, string StatusMessage)` | `GameStateDto { readonly cells: readonly (readonly CellValue[])[]; readonly status: GameStatusType; readonly currentTurn: PlayerSymbolType \| null; readonly winner: PlayerSymbolType \| null; readonly winningLine: WinningLineDto \| null; readonly isPlayerXConnected: boolean; readonly isPlayerOConnected: boolean; readonly hasPlayerXAcceptedRematch: boolean; readonly hasPlayerOAcceptedRematch: boolean; readonly statusMessage: string; }` | Canonical snapshot of the match state |
| `JoinGameResponseDto(bool IsJoined, string? AssignedSymbol, string? RejectionReason, GameStateDto GameState)` | `JoinGameResponseDto { readonly isJoined: boolean; readonly assignedSymbol: PlayerSymbolType \| null; readonly rejectionReason: string \| null; readonly gameState: GameStateDto; }` | Returned directly from `JoinGame` Hub RPC |
| `MakeMoveRequestDto(int Row, int Col)` | `MakeMoveRequestDto { readonly row: number; readonly col: number; }` | Sent by client when clicking a cell |
| `MoveResultDto(bool IsAccepted, string ErrorCode, string? ErrorMessage, int Row, int Col, string? Symbol, GameStateDto GameState)` | `MoveResultDto { readonly isAccepted: boolean; readonly errorCode: MoveErrorCodeType; readonly errorMessage: string \| null; readonly row: number; readonly col: number; readonly symbol: PlayerSymbolType \| null; readonly gameState: GameStateDto; }` | Returned to caller and broadcast on move |
| `PlayerAssignmentDto(string AssignedSymbol, GameStateDto GameState)` | `PlayerAssignmentDto { readonly assignedSymbol: PlayerSymbolType; readonly gameState: GameStateDto; }` | Sent when symbols are assigned/swapped on rematch or disconnect promotion |
| `RematchResponseDto(bool IsAccepted, string? ErrorMessage, bool DidRematchStart, GameStateDto GameState)` | `RematchResponseDto { readonly isAccepted: boolean; readonly errorMessage: string \| null; readonly didRematchStart: boolean; readonly gameState: GameStateDto; }` | Returned to caller on `RequestRematch` |

---

### SignalR Hub RPCs & Broadcast Events (server/Hubs/GameHub.cs)

- **Strongly Typed Client Contract (`IGameClient` colocated in server/Hubs/GameHub.cs):**
  - `Task OnGameStateUpdated(GameStateDto gameState);` — Broadcasts atomic board, turn, win/draw, or rematch vote updates to participants.
  - `Task OnPlayerAssignmentChanged(PlayerAssignmentDto assignment);` — Sent to a specific participant when their symbol changes (e.g., symbol swap upon mutual rematch or promotion from `O` to waiting `X` upon opponent disconnect).
  - `Task OnMoveRejected(MoveResultDto rejection);` — Sent to the caller when an attempted move is invalid.
- **Client-to-Server Hub Invocations (`GameHub : Hub<IGameClient>`):**
  - `Task<JoinGameResponseDto> JoinGame()` — Attempts to seat `Context.ConnectionId` in the match. If the second player joins and starts the match, broadcasts `OnGameStateUpdated` to both participants.
  - `Task<MoveResultDto> MakeMove(MakeMoveRequestDto request)` — Validates and applies a move for `Context.ConnectionId`. If valid, broadcasts `OnGameStateUpdated` to both participants; if invalid, invokes `OnMoveRejected` on caller and returns the rejection DTO without altering the board.
  - `Task<RematchResponseDto> RequestRematch()` — Registers a rematch vote for `Context.ConnectionId`. If one player has voted, broadcasts `OnGameStateUpdated` so both clients see `(1/2)` rematch status. If both players have voted, swaps symbols (`X <-> O`), resets the board, sends `OnPlayerAssignmentChanged` individually to Player X and Player O with their swapped symbols, and broadcasts the new `GameStateDto`.
  - `override Task OnDisconnectedAsync(Exception? exception)` — Delegates disconnect cleanup to `IGameSessionService`. If an active participant disconnected and an opponent remains, sends `OnPlayerAssignmentChanged` (with `AssignedSymbol = "X"` and `AbortedByDisconnect` state message) to the remaining player so they are immediately ready for a returning/new second player.

---

## 4. Server Architecture & DI Wiring

- **Interfaces & Services (server/Services/GameSessionService.cs):**
  - `IGameSessionService` is colocated above `GameSessionService` in server/Services/GameSessionService.cs.
  - Holds `private readonly Lock _syncRoot = new();` and `private readonly GameMatch _match = new();`.
  - Every public service method acquires `lock (_syncRoot)` before reading or mutating `_match` and constructs immutable DTO snapshots inside the lock so Hub methods can safely await SignalR outbound messages outside the lock.
  - Exposes:
    - `JoinSession(string connectionId)`
    - `MakeMove(string connectionId, int row, int col)`
    - `RequestRematch(string connectionId)`
    - `HandleDisconnect(string connectionId)`
    - `GetParticipantConnectionIds()`
- **DI Registration & LAN Hosting (server/Program.cs):**
  - Registers `builder.Services.AddSingleton<IGameSessionService, GameSessionService>();` and `builder.Services.AddSignalR();`.
  - Configures CORS for local Vite development (`http://localhost:5173`) and binds Kestrel so LAN clients can reach the server via the host machine's LAN IP.
  - Configures static file serving (`UseDefaultFiles`, `UseStaticFiles`, `MapFallbackToFile("index.html")`) pointing to the built client assets (configured to copy/output into server/wwwroot/ during build or served from client/dist/ when present).
  - Maps the SignalR hub at `/hubs/game`.

---

## 5. Client Architecture & Phaser 4 Presentation

- **Network Service Integration (client/src/services/GameNetworkService.ts):**
  - Encapsulates `@microsoft/signalr` `HubConnection` configured with `withAutomaticReconnect()`.
  - Never imported as raw SignalR inside Phaser scenes (preserving rule `TS-ARCH003`).
  - Exposes strongly typed methods:
    - `connectAndJoinAsync(): Promise<JoinGameResponseDto>`
    - `retryJoinAsync(): Promise<JoinGameResponseDto>`
    - `makeMoveAsync(row: number, col: number): Promise<MoveResultDto>`
    - `requestRematchAsync(): Promise<RematchResponseDto>`
    - Event subscription helpers: `onGameStateUpdated`, `offGameStateUpdated`, `onPlayerAssignmentChanged`, `offPlayerAssignmentChanged`, `onMoveRejected`, `offMoveRejected`, `onConnectionStateChanged`, `offConnectionStateChanged`.
- **Phaser Scenes & UI (client/src/scenes/BoardScene.ts & client/src/ui/BoardHud.ts):**
  - client/src/main.ts configures `Phaser.Game` (`800x600`, `Phaser.Scale.FIT`, `Phaser.Scale.CENTER_BOTH`) and registers `BoardScene`.
  - `BoardScene` manages visual presentation and pointer input:
    - Resets all per-session state in `init()`.
    - Renders the top HUD via `BoardHud`:
      - **Connection Status Pill:** `"Connected (LAN)"`, `"Reconnecting..."`, or `"Disconnected — Check Server Connection"`.
      - **Assigned Symbol Badge:** `"Your Symbol: X"`, `"Your Symbol: O"`, or `"Spectator (Room Busy)"`.
      - **Primary Status Banner:** Clearly communicates waiting state, active turn (`"Your Turn (X)"` vs `"Opponent's Turn (O)"`), match outcome (`"You Win! (X)"`, `"Opponent Wins (O)"`, `"Match Draw!"`), rejection feedback, or disconnect notice (`"Opponent disconnected. Waiting for a new opponent..."`).
    - Renders the **3x3 Interactive Grid**:
      - 9 clickable cell zones (`row: 0..2`, `col: 0..2`) with hover highlight when the local player is a participant, the match is `InProgress`, it is the local player's turn, and the cell is `Empty`.
      - Symbol graphics/text (`X` in coral red `#ff5964`, `O` in sky blue `#35a7ff`) animated with a smooth scale-in tween (`Back.easeOut`) when newly placed.
      - Winning line overlay drawn across the 3 winning coordinates (`WinningLineDto`) when `status === 'Won'`.
    - Renders **Contextual Action Buttons**:
      - **Rematch Button:** Displayed when `status === 'Won' || status === 'Draw'`. Shows `"Accept Rematch"` before clicking and `"Waiting for Opponent (1/2)..."` after clicking.
      - **Retry Join Button:** Displayed when `isJoined === false` (3rd client rejected because the match is full) or after connection loss, allowing the user to request joining when a slot opens.
  - **Mandatory `SHUTDOWN` Cleanup:**
    - Registers `this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => { ... })` to unsubscribe all `GameNetworkService` listeners, kill active tweens (`this.tweens.killAll()`), and clear timers (`this.time.removeAllEvents()`).

---

## 6. Edge Cases, Concurrency & Lifecycle Invariants

- **Authoritative Validation:** Even if a client modifies local state or clicks rapidly, `GameMatch.TryMakeMove` validates participant identity, `InProgress` status, turn order, `0..2` bounds, and cell emptiness inside `GameSessionService`'s lock. Rejected moves never mutate `_cells` or `CurrentTurn`.
- **Disconnects & Abandonment:**
  - Disconnecting mid-match or during post-game rematch voting immediately clears the board, aborts the match (`AbortedByDisconnect`), promotes the remaining player to waiting Player `X`, and notifies them.
  - When the disconnected player (or another client) connects and invokes `JoinGame()`, they fill the vacant `PlayerOConnectionId` slot, transitioning the match immediately to `InProgress` with an empty board and `CurrentTurn = PlayerSymbol.X`.
- **Third Client Isolation:**
  - While both `PlayerXConnectionId` and `PlayerOConnectionId` are occupied (whether `InProgress`, `Won`, or `Draw` during rematch negotiation), any 3rd client calling `JoinGame()`, `MakeMove()`, or `RequestRematch()` is rejected cleanly without mutating the match or receiving participant-only symbol assignments.
- **Race Conditions:**
  - Simultaneous `JoinGame`, `MakeMove`, `RequestRematch`, and `OnDisconnectedAsync` calls are serialized via `_syncRoot` in `GameSessionService`.

---

## 7. Implementation Plan (Phases & Steps)

### Phase 1: Solution Scaffolding, Pure Domain Model & xUnit Tests
- [x] **Step 1.1:** Create .NET 10 solution (`NetworkingTicTacToe.sln`), server project (server/NetworkingTicTacToe.Server.csproj), and xUnit test project (tests/NetworkingTicTacToe.Tests.csproj).
  - Files: NetworkingTicTacToe.sln, server/NetworkingTicTacToe.Server.csproj, tests/NetworkingTicTacToe.Tests.csproj
  - Verification: `dotnet build`
- [x] **Step 1.2:** Implement pure domain models partitioned into descriptive subdomains under server/Domain/ (`Board/`, `Match/`, `Results/`).
  - Files: server/Domain/Board/Board.cs, server/Domain/Board/BoardCoordinate.cs, server/Domain/Board/CellState.cs, server/Domain/Board/WinningLine.cs, server/Domain/Match/GameMatch.cs, server/Domain/Match/GameStatus.cs, server/Domain/Match/MoveErrorCode.cs, server/Domain/Match/PlayerSymbol.cs, server/Domain/Results/DisconnectResult.cs, server/Domain/Results/JoinMatchResult.cs, server/Domain/Results/MoveAttemptResult.cs, server/Domain/Results/RematchVoteResult.cs
  - Verification: `dotnet build`
- [x] **Step 1.3:** Implement comprehensive xUnit tests mirroring domain structure under tests/Domain/ (`Board/`, `Match/`) covering player joining, 3rd player rejection, turn order, move validation (out-of-turn, occupied cell, out-of-bounds, post-game, non-participant), all 8 winning lines (3 rows, 3 cols, 2 diagonals), full-board draws, mutual rematch with symbol swap, and disconnect recovery.
  - Files: tests/Domain/Board/BoardTests.cs, tests/Domain/Match/GameMatchTests.cs
  - Verification: `dotnet test`

### Phase 2: Server DTOs, Session Service, SignalR Hub & LAN Hosting
- [x] **Step 2.1:** Create immutable SignalR DTO records and thread-safe `GameSessionService` with colocated `IGameSessionService`.
  - Files: server/DTOs/GameContracts.cs, server/Services/GameSessionService.cs
  - Verification: `dotnet build`
- [x] **Step 2.2:** Implement `GameHub` (`Hub<IGameClient>`) and wire DI, Kestrel LAN configuration, and static asset hosting in server/Program.cs, plus xUnit service concurrency/flow tests.
  - Files: server/Hubs/GameHub.cs, server/Program.cs, server/Properties/launchSettings.json, tests/Services/GameSessionServiceTests.cs
  - Verification: `dotnet build` & `dotnet test`

### Phase 3: Client Scaffolding & SignalR Network Service
- [x] **Step 3.1:** Scaffold Vite + TypeScript (`strict: true`) + Phaser 4 + `@microsoft/signalr` project in client/ and configure Vite build output / dev proxy.
  - Files: client/package.json, client/tsconfig.json, client/vite.config.ts, client/index.html
  - Verification: `npm --prefix client install` & `npm --prefix client run build`
- [x] **Step 3.2:** Implement synchronized TypeScript DTO interfaces in client/src/types/GameContracts.ts and `GameNetworkService` in client/src/services/GameNetworkService.ts.
  - Files: client/src/types/GameContracts.ts, client/src/services/GameNetworkService.ts
  - Verification: `npm --prefix client run build`

### Phase 4: Phaser 4 BoardScene, HUD & Interactive UX
- [x] **Step 4.1:** Implement `BoardHud` UI helper and `BoardScene` with 3x3 interactive grid, X/O placement tweens, winning line highlight, rematch button, retry-join button, connection status banner, and `SHUTDOWN` listener cleanup.
  - Files: client/src/ui/BoardHud.ts, client/src/scenes/BoardScene.ts, client/src/main.ts
  - Verification: `npm --prefix client run build`

### Phase 5: Static Asset Hosting Integration & Pre-Commit Verification Gate
- [ ] **Step 5.1:** Verify ASP.NET Core serves the built Phaser bundle from client/dist/ (or server/wwwroot/) and run the full pre-commit verification gate (`dotnet build`, `dotnet test`, `npm --prefix client run build`, and .agents/scripts/check-architecture.ps1).
  - Files: server/Program.cs, .agents/context/implementations/summaries/mvp-summary.md
  - Verification: `dotnet test`, `npm --prefix client run build`, `powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1`

---

## 8. Architectural Self-Review & Verification Acceptance Criteria

### 4-Tier Architectural Self-Review
- **Blocker Risks:** None. Pure domain logic in server/Domain/ is completely isolated from SignalR (`CS-ARCH005`), and Phaser scenes never instantiate `HubConnectionBuilder` (`TS-ARCH003`).
- **Major Risks:** Race conditions between simultaneous moves/disconnects mitigated via `_syncRoot` locking inside `GameSessionService` and immutable DTO snapshot creation before broadcasting.
- **Minor Risks:** Stale Phaser scene state across rematches mitigated by resetting state in `init()` and updating board state reactively from authoritative `GameStateDto` payloads, plus mandatory unsubscription on `Phaser.Scenes.Events.SHUTDOWN`.
- **Nit Risks:** All C# methods strictly use `{}` block syntax (`CS-ARCH003`), private fields use `_camelCase`, constants use `UPPER_SNAKE_CASE`, and TypeScript uses zero `any` (`TS-ARCH001`).

### Verification & Acceptance Criteria
- [ ] **Scenario 1 (LAN Two-Player Match):** Two players on separate browser clients connect over LAN and complete a match with both 3x3 boards staying 100% synchronized.
- [ ] **Scenario 2 (Illegal Move Rejection):** Moves attempted out of turn, on an occupied cell, out of bounds (`< 0` or `>= 3`), by a non-participant, or after game completion are rejected without mutating board state.
- [ ] **Scenario 3 (Win & Draw Detection):** Server detects all 8 winning configurations (3 rows, 3 columns, 2 diagonals) and full-board draws, immediately blocking subsequent moves.
- [ ] **Scenario 4 (Disconnect Handling & Return):** Closing a client or losing network connection aborts the active match, displays a notification on the remaining client, retains the remaining client as waiting Player `X`, and starts a new match when a second player returns/connects.
- [ ] **Scenario 5 (Mutual Rematch & Third-Client Exclusion):** Mutual rematch acceptance swaps symbols (`X <-> O`) and creates a fresh empty 3x3 board with `X` moving first; a third connecting client receives a busy/full message and cannot join the active session.
- [ ] **Automated Gate:** `dotnet build` (0 warnings/errors), `dotnet test` (all xUnit tests pass), `npm --prefix client run build` (0 `tsc`/Vite errors), and `powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1` (0 violations).
