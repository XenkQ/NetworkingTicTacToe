# SignalR Protocol and Network Transport System Documentation

## Purpose

Use when: modifying, extending, or reviewing SignalR Hub RPC signatures, server-to-client push events, modular C# and TypeScript DTO contracts, LAN CORS / Vite WebSocket proxy routing, client reconnect behavior, or `stateVersion` ordering guards.

Responsibilities across the repository:
- Owns the strongly typed SignalR hub (`GameHub : Hub<IGameClient>`) and client push contract (`IGameClient`) in server/Hubs/GameHub.cs, mapped at `/hubs/game` in server/Program.cs.
- Owns LAN/local-dev CORS policy (`LanCorsPolicy`), `/health` endpoint, and static client bundle hosting in server/Program.cs, plus dev WebSocket proxy rules in client/vite.config.ts.
- Owns modular DTO contracts synchronized across server/DTOs/ and client/src/types/.
- Owns client `HubConnection` lifecycle, automatic reconnection, typed RPC wrappers, listener subscriptions, and monotonic `stateVersion` filtering in client/src/services/GameNetworkService.ts.

Not responsible for:
- Turn order, board validation, win/draw detection, or seat-swap rules (owned by server/Domain/ and coordinated by server/Services/GameSessionService.cs).
- Phaser 4 grid rendering, tweens, or HUD button visuals (owned by client/src/scenes/BoardScene.ts and client/src/ui/BoardHud.ts).

## Reading Map

- Primary server locations:
  - server/Hubs/GameHub.cs
  - server/Program.cs
  - server/DTOs/GameStateDtos.cs
  - server/DTOs/JoinGameResponseDto.cs
  - server/DTOs/MoveDtos.cs
  - server/DTOs/PlayerAssignmentDto.cs
  - server/DTOs/RematchResponseDto.cs
- Primary client locations:
  - client/src/services/GameNetworkService.ts
  - client/src/types/GameDomainTypes.ts
  - client/src/types/GameStateDtos.ts
  - client/src/types/JoinGameResponseDto.ts
  - client/src/types/MoveDtos.ts
  - client/src/types/PlayerAssignmentDto.ts
  - client/src/types/RematchResponseDto.ts
  - client/src/types/index.ts
  - client/vite.config.ts
- Primary test locations:
  - tests/Hubs/GameHubTests.cs
- Related docs:
  - AGENTS.md
  - .agents/context/shared-coding-standards.md
  - .agents/context/technology-documentation.md
  - .agents/context/game-systems/matchmaking-and-session-system.md
  - .agents/context/game-systems/phaser-board-ui-system.md

## Architecture, Protocol, and Data Flow

- Core components & DI registrations:
  - Server: server/Program.cs calls `builder.Services.AddSignalR()`, binds `http://0.0.0.0:5079` by default when no URL config is set, applies `LanCorsPolicy` (allowing `localhost`, loopback, and private IPv4 LAN ranges `10.*`, `172.16-31.*`, `192.168.*`, `169.254.*` with `.AllowCredentials()`), serves built assets from client/dist (or server/wwwroot fallback), exposes `GET /health`, and maps `app.MapHub<GameHub>("/hubs/game")`.
  - `GameHub` receives `IGameSessionService` and `ILogger<GameHub>` via constructor injection (transient per-invocation SignalR hub lifetime).
  - Client: client/src/main.ts creates a single `GameNetworkService` instance (`DEFAULT_HUB_URL = '/hubs/game'`) and injects it as `IGameNetworkService` into `BoardScene`. During Vite dev (`port: 5173`), client/vite.config.ts proxies `/hubs` (`ws: true`, `changeOrigin: true`) and `/health` to `http://localhost:5079`.
- Key interfaces & DTO contracts (server/DTOs/ <-> client/src/types/):
  - ASP.NET Core SignalR serializes C# `PascalCase` positional records into JSON `camelCase` properties consumed by TypeScript `readonly` interfaces:
    - server/DTOs/GameStateDtos.cs <-> client/src/types/GameStateDtos.ts: `BoardCellCoordinateDto`, `WinningLineDto`, `GameStateDto` (`stateVersion`, `cells`, `status`, `currentTurn`, `winner`, `winningLine`, `isPlayerXConnected`, `isPlayerOConnected`, `hasPlayerXAcceptedRematch`, `hasPlayerOAcceptedRematch`, `statusMessage`).
    - server/DTOs/JoinGameResponseDto.cs <-> client/src/types/JoinGameResponseDto.ts: `JoinGameResponseDto` (`isJoined`, `assignedSymbol`, `rejectionReason`, `gameState`).
    - server/DTOs/MoveDtos.cs <-> client/src/types/MoveDtos.ts: `MakeMoveRequestDto` (`row`, `col`), `MoveResultDto` (`isAccepted`, `errorCode`, `errorMessage`, `row`, `col`, `symbol`, `gameState`).
    - server/DTOs/PlayerAssignmentDto.cs <-> client/src/types/PlayerAssignmentDto.ts: `PlayerAssignmentDto` (`assignedSymbol`, `gameState`).
    - server/DTOs/RematchResponseDto.cs <-> client/src/types/RematchResponseDto.ts: `RematchResponseDto` (`isAccepted`, `errorMessage`, `didRematchStart`, `gameState`).
    - client/src/types/GameDomainTypes.ts: string literal union types (`CellValue`, `PlayerSymbolType`, `GameStatusType`, `MoveErrorCodeType`, `ConnectionStateType`), re-exported via client/src/types/index.ts.
- SignalR RPC & broadcast sequence (verify exact dispatch order in server/Hubs/GameHub.cs):
  1. **Client -> Server `JoinGame()`**:
     - Invokes `_sessionService.JoinSession(Context.ConnectionId)`.
     - If `result.DidMatchStart` is `true` (2nd player joined), awaits `Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState)` before returning `JoinGameResponseDto` to the caller.
  2. **Client -> Server `MakeMove(MakeMoveRequestDto request)`**:
     - Validates `ArgumentNullException.ThrowIfNull(request)` and calls `_sessionService.MakeMove`.
     - If `result.IsAccepted` is `true`, awaits `Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState)`.
     - If `result.IsAccepted` is `false`, awaits `Clients.Caller.OnMoveRejected(result.Response)` (not broadcast to opponent).
     - Returns `MoveResultDto` to the caller.
  3. **Client -> Server `RequestRematch()`**:
     - Calls `_sessionService.RequestRematch(Context.ConnectionId)`.
     - If `result.DidRematchStart` is `true` (2/2 accepted): dispatches `OnPlayerAssignmentChanged` to `NewPlayerXConnectionId` and `NewPlayerOConnectionId` in parallel via `Task.WhenAll`, then awaits `Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState)`.
     - Else if `result.Response.IsAccepted && result.DidVoteChange` (first 1/2 vote): awaits `Clients.Clients(result.ParticipantConnectionIds).OnGameStateUpdated(result.Response.GameState)`. Duplicate votes (`DidVoteChange == false`) skip broadcasting.
     - Returns `RematchResponseDto` to the caller.
  4. **Client -> Server `GetGameState()`**:
     - Returns `_sessionService.GetCurrentGameState()` without broadcasting.
  5. **Transport Disconnect `OnDisconnectedAsync(Exception? exception)`**:
     - Calls `_sessionService.HandleDisconnect(Context.ConnectionId)`.
     - If `result.WasParticipant` and `result.RemainingPlayerConnectionId` is non-null, awaits `Clients.Client(result.RemainingPlayerConnectionId).OnPlayerAssignmentChanged(result.RemainingPlayerAssignment)` (promoting remaining player to `"X"` in `AbortedByDisconnect` state), then calls `base.OnDisconnectedAsync(exception)`.
- Runtime flow on the client (`GameNetworkService` in client/src/services/GameNetworkService.ts):
  - Configures `HubConnectionBuilder` with `.withUrl(hubUrl)` and `.withAutomaticReconnect([0, 1000, 2000, 5000, 10000])`.
  - `ensureConnectedAsync()` starts a disconnected connection (emitting `'Connecting'` -> `'Connected'`) or polls `waitForStateAsync` (`50ms` interval, `5000ms` timeout) when already transitioning (`Connecting`, `Reconnecting`, `Disconnecting`).
  - Incoming `OnGameStateUpdated`, `OnPlayerAssignmentChanged`, and RPC return payloads pass through `tryUpdateLatestGameState(incomingState)`, which rejects any snapshot with `incomingState.stateVersion < _latestGameState.stateVersion`. `OnPlayerAssignmentChanged` additionally guards against `assignment.gameState.stateVersion < _latestAssignmentVersion`.
  - On `onreconnecting` and `onclose`, `resetSessionState()` clears `_isJoined`, `_assignedSymbol`, `_latestGameState`, and `_latestAssignmentVersion = 0`.
  - On `onreconnected`, sets state to `'Connected'` and calls `rejoinAfterReconnect()` (`connectAndJoinAsync()`), emitting `onSessionJoined` to active listeners or falling back to `'Disconnected'` if rejoin fails.

## Rules and Invariants

- Authoritative validation rules:
  - Hub RPC methods must never trust client-supplied symbols or status; identity derives strictly from `Context.ConnectionId` and coordinates from `MakeMoveRequestDto`.
  - Illegal gameplay requests return structured `MoveResultDto` / `RematchResponseDto` / `JoinGameResponseDto` payloads instead of throwing hub exceptions that drop or fault client state.
- Ordering, concurrency, or state transition guarantees:
  - During rematch start, `OnPlayerAssignmentChanged` is awaited before the shared `OnGameStateUpdated` broadcast, and both carry the same post-reset `stateVersion` so clients update their swapped symbol (`X` <-> `O`) without transient turn-banner mismatches.
  - Because server push events (`OnGameStateUpdated`) can arrive at the client before the originating `invoke(...)` promise resolves, `GameNetworkService` always reconciles `effectiveResponse` / `effectiveState` using `_latestGameState` after `tryUpdateLatestGameState`.
- Constraints contributors must preserve:
  - Maintain modular DTO parity between server/DTOs/ and client/src/types/; never consolidate DTOs back into a single monolithic file.
  - Never instantiate `HubConnectionBuilder` inside Phaser scenes (`TS-ARCH003`); all SignalR transport stays encapsulated in client/src/services/.
  - Listener callbacks in `GameNetworkService` are wrapped in `try/catch` blocks so one failing subscriber cannot break other subscribers.
  - Ask the user before changing Hub RPC method names, `IGameClient` event names, `/hubs/game` URL routing, or DTO payload fields.

## Extension Points

- Safe extension areas:
  - Adding new strongly typed client push methods on `IGameClient` paired with `on<Event>` / `off<Event>` subscription Sets on `IGameNetworkService` and `GameNetworkService`.
  - Adding new modular DTO pairs in server/DTOs/ and client/src/types/ (and re-exporting from client/src/types/index.ts).
  - Switching participant targeting from `Clients.Clients(result.ParticipantConnectionIds)` to SignalR `Groups` (`Groups.AddToGroupAsync` / `RemoveFromGroupAsync`) if multi-room matchmaking or live spectator broadcasting is introduced.
- Required dependencies and contracts:
  - Keep Hub RPC method names free of the `*Async` suffix (per server coding standards for SignalR RPC contract stability) while client wrapper methods in `IGameNetworkService` retain `*Async` (`connectAndJoinAsync`, `makeMoveAsync`, `requestRematchAsync`, `getGameStateAsync`, `disconnectAsync`).
- Automated xUnit & client testing implications:
  - Update `RecordingGameClient` and `RecordingHubCallerClients` in tests/Hubs/GameHubTests.cs whenever `IGameClient` changes, and run `dotnet test` plus `npm --prefix client run build` to verify cross-stack type safety.

## Integration Notes

- Upstream dependencies:
  - Server: `IGameSessionService` (server/Services/GameSessionService.cs) and `Microsoft.AspNetCore.SignalR`.
  - Client: `@microsoft/signalr` (`^10.0.11`) and DTO interfaces in client/src/types/.
- Downstream consumers:
  - `BoardScene` (client/src/scenes/BoardScene.ts) consumes `IGameNetworkService`.
- Cross-layer coupling risks:
  - String-based RPC names in `GameNetworkService` (`'JoinGame'`, `'MakeMove'`, `'RequestRematch'`, `'GetGameState'`, `'OnGameStateUpdated'`, `'OnPlayerAssignmentChanged'`, `'OnMoveRejected'`) must match C# method names on `GameHub` and `IGameClient` case-sensitively.
  - Private IPv6 LAN addresses (other than `::1` loopback) are not matched by `IsAllowedLanOrLocalDevOrigin` in server/Program.cs, which checks IPv4 RFC1918/link-local ranges (`AddressFamily.InterNetwork`).

## Known Risks and Open Questions

- Known limitations:
  - Non-joined room-busy connections (`IsJoined == false`) are excluded from `ParticipantConnectionIds`, so they do not receive live `OnGameStateUpdated` broadcasts while waiting and only refresh state when clicking `Retry Join`.
  - `waitForStateAsync` in `GameNetworkService` uses a 5000 ms timeout; if automatic reconnect takes longer (e.g., the 5000 ms or 10000 ms retry steps) and the user clicks an action mid-reconnect, `ensureConnectedAsync` will throw a timeout error surfaced to the HUD feedback banner.
- Open design questions:
  - None for the current LAN/local single-room protocol.
- Suggested follow-up tasks:
  - Verify live two-device LAN origin acceptance (`http://<lan-ip>:5079`) and automatic reconnect recovery in browsers (currently unverified in this documentation pass).
