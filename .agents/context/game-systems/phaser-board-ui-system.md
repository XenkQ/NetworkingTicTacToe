# Phaser Client Scene and HUD UI System Documentation

## Purpose

Use when: modifying, extending, or debugging Phaser 4 game configuration, 3x3 board grid rendering, cell hover/click input handling, symbol and winning-line animations, canvas HUD status banners/buttons, or scene `SHUTDOWN` resource cleanup on the client.

Responsibilities across the repository:
- Owns client composition root and `Phaser.Game` configuration (`800x600`, `Phaser.Scale.FIT`, `Phaser.Scale.CENTER_BOTH` inside `#game-container`) in client/src/main.ts and client/index.html.
- Owns `BoardScene` (`key: 'BoardScene'`) in client/src/scenes/BoardScene.ts: 3x3 interactive cell grid, local input gating, pending-request locks, animated `X`/`O` symbol placement, winning-line `Graphics` overlay, monotonic `stateVersion` checks, and deterministic `SHUTDOWN` cleanup.
- Owns `BoardHud` in client/src/ui/BoardHud.ts: canvas-rendered connection pill, local symbol badge, primary turn/outcome banner, auto-expiring feedback messages, and contextual `Accept Rematch` / `Retry Join` buttons.

Not responsible for:
- Constructing or managing SignalR `HubConnection` instances (owned by client/src/services/GameNetworkService.ts; banned in scenes by `TS-ARCH003`).
- Authoritative move legality, turn switching, win/draw evaluation, or rematch vote counting (owned by the server; client never mutates board state optimistically).

## Reading Map

- Primary server locations:
  - server/Program.cs (hosts production static client assets from client/dist when built)
- Primary client locations:
  - client/src/main.ts
  - client/src/scenes/BoardScene.ts
  - client/src/ui/BoardHud.ts
  - client/index.html
  - client/src/services/GameNetworkService.ts
  - client/src/types/index.ts
- Primary test locations:
  - No automated client unit test runner is currently configured; validated via `npm --prefix client run build` (`tsc && vite build`) and .agents/scripts/check-architecture.ps1.
- Related docs:
  - AGENTS.md
  - .agents/context/client-coding-standards.md
  - .agents/context/technology-documentation.md
  - .agents/skills/phaser-core/SKILL.md
  - .agents/context/game-systems/signalr-network-protocol-system.md

## Architecture, Protocol, and Data Flow

- Core components & DI registrations:
  - client/src/main.ts instantiates `export const gameNetworkService = new GameNetworkService()`, injects it via constructor into `new BoardScene(gameNetworkService)`, and boots `export const game = new Phaser.Game(config)`.
  - `BoardScene` instantiates `BoardHud` inside `create()`, passing `BoardHudCallbacks` (`onRematchClicked`, `onRetryJoinClicked`).
  - All UI elements in both `BoardScene` and `BoardHud` are Phaser 4 canvas `GameObjects` (`Rectangle`, `Zone`, `Text`, `Arc`, `Graphics`, `Container`) positioned within the `800x600` logical coordinate space—there are no external HTML DOM HUD overlays.
- Key interfaces & DTO contracts:
  - Consumes `IGameNetworkService` from client/src/services/GameNetworkService.ts and readonly DTO/domain types (`GameStateDto`, `WinningLineDto`, `JoinGameResponseDto`, `MoveResultDto`, `PlayerAssignmentDto`, `PlayerSymbolType`, `ConnectionStateType`) from client/src/types/.
  - Defines `BoardHudCallbacks` in client/src/ui/BoardHud.ts for upward button-click delegation back to `BoardScene`.
- SignalR RPC & broadcast sequence:
  - Subscribes in `BoardScene.create()` to five `IGameNetworkService` streams:
    1. `onGameStateUpdated` -> `handleGameStateUpdated`
    2. `onPlayerAssignmentChanged` -> `handlePlayerAssignmentChanged`
    3. `onMoveRejected` -> `handleMoveRejected`
    4. `onConnectionStateChanged` -> `handleConnectionStateChanged`
    5. `onSessionJoined` -> `handleSessionJoined`
  - Dispatches player intent through `_networkService.connectAndJoinAsync()`, `_networkService.makeMoveAsync(row, col)`, `_networkService.requestRematchAsync()`, and `_networkService.retryJoinAsync()`.
- Runtime flow (verify coordinates, colors, and guards in client/src/scenes/BoardScene.ts and client/src/ui/BoardHud.ts):
  1. **Scene Initialization & Lifecycle (`init` -> `create` -> `SHUTDOWN`)**:
     - `init()` resets all per-session arrays and flags (`_symbols`, `_cellBgs`, `_cellZones`, `_winningLineGraphics`, `_isMovePending`, `_isRematchPending`, `_latestState`, `_latestAssignmentVersion`, `_localSymbol`, `_isJoined`).
     - `create()` builds the 3x3 grid (`CELL_SIZE = 110`, `CELL_GAP = 12`, centered at `(400, 310)` -> `COL_CENTERS = [278, 400, 522]`, `ROW_CENTERS = [188, 310, 432]`), creates `BoardHud`, registers all 5 network subscriptions, attaches a `Phaser.Scenes.Events.SHUTDOWN` cleanup handler, and triggers `connectAndJoin()`.
  2. **Local Input Gating & Move Dispatch (`handleCellClick`)**:
     - Hover highlight (`POINTER_OVER`) activates only when `_isJoined`, `_localSymbol !== null`, `status === 'InProgress'`, `currentTurn === _localSymbol`, target cell is `'Empty'`, and `!_isMovePending`.
     - Click (`POINTER_DOWN`) checks `_isMovePending` (silent return if locked), `connectionState === 'Connected'`, `_isJoined && _localSymbol !== null`, `status === 'InProgress'`, `currentTurn === _localSymbol`, and `cells[row][col] === 'Empty'`, showing targeted `BoardHud.showFeedback(..., true)` messages when blocked.
     - Valid local intent sets `_isMovePending = true` and calls `_networkService.makeMoveAsync(row, col)`, releasing `_isMovePending = false` in `.then()`, `.catch()`, `handleMoveRejected`, and disconnect/reconnect transitions without mutating `_symbols` locally.
  3. **Authoritative State Rendering (`applyGameState`)**:
     - Diffs `state.cells[r][c]` against `_symbols[r][c]`: destroys text objects when cells reset to `'Empty'`, or spawns `renderSymbolWithAnimation` (`'X'` in `#ff5964`, `'O'` in `#35a7ff`, `Back.easeOut` scale `0 -> 1` in `220ms`) for new/changed symbols.
     - When `state.status === 'Won'` and `state.winningLine.cells.length >= 3`, draws an `8px` gold (`0xffe066`) line extending `35px` past the first and third winning cell centers (`Sine.easeOut` alpha `0 -> 1` in `250ms`) and highlights winning cell strokes; clears the overlay in all non-`Won` states.
     - Delegates status/button updates to `BoardHud.updateGameState(state, _localSymbol, _isJoined)`.
  4. **HUD State & Contextual Action Buttons (`BoardHud`)**:
     - Top bar (`y = 28`): connection status pill at `x = 185` (`Connected (LAN)`, `Connecting...`, `Reconnecting...`, `Disconnected — Check Server Connection`) and symbol badge at `x = 665` (`Your Symbol: X`, `Your Symbol: O`, `Spectator (Room Busy)`, `Offline`, etc.).
     - Primary banner (`y = 78`) and feedback banner (`y = 112`, auto-cleared after `4000ms` via `scene.time.delayedCall`).
     - Action button slot (`x = 400, y = 538`): shows `Retry Join` when `!isJoined` (room full) or `Disconnected`; shows `Accept Rematch` when joined and `status` is `'Won'` or `'Draw'` (displaying `'Accept Rematch'`, `'Accept Rematch (1/2 Ready)'`, or disabled `'Waiting for Opponent (1/2)...'` based on `hasPlayerXAcceptedRematch` / `hasPlayerOAcceptedRematch`); hides both during active play or `Reconnecting`.

## Rules and Invariants

- Authoritative validation rules:
  - Never render a player's `X` or `O` symbol in `handleCellClick` before the server returns/broadcasts an updated `GameStateDto`. All board visuals derive strictly from `applyGameState(state)`.
- Ordering, concurrency, or state transition guarantees:
  - `BoardScene` enforces `isNewerOrEqualState` (`incoming.stateVersion >= _latestState.stateVersion`) and `_latestAssignmentVersion` checks so duplicate or delayed callbacks cannot overwrite newer board or symbol state.
  - `_isMovePending` and `_isRematchPending` must always be reset on every resolution, rejection, catch, and connection-loss path so player input never deadlocks.
  - On `'Disconnected'` or `'Reconnecting'`, `handleConnectionStateChanged` resets `_isJoined`, `_localSymbol`, `_latestState`, and `_latestAssignmentVersion = 0` so fresh state versions after reconnect or server restart are accepted cleanly.
- Constraints contributors must preserve:
  - Maintain strict TypeScript (`"strict": true`, zero `any` per `TS-ARCH001`, zero `@ts-ignore` / `@ts-nocheck` per `TS-ARCH002`).
  - Always clean up every network listener, `Phaser.Time.TimerEvent` (`_feedbackTimer` and `this.time.removeAllEvents()`), active tween (`this.tweens.killAll()`), and HUD container (`this._hud?.destroy()`) inside `Phaser.Scenes.Events.SHUTDOWN`.
  - Ask the user before changing the `800x600` canvas layout, color palette, cell dimensions, or HUD messaging conventions.

## Extension Points

- Safe extension areas:
  - Adding sound effects, particle effects, or hover/turn tweens inside `BoardScene` driven by transitions in `applyGameState` and `handleMoveRejected`.
  - Adding new scenes (e.g., `LobbyScene` or `TitleScene`) registered in client/src/main.ts that share the injected `IGameNetworkService` instance and rely on `BoardScene`'s existing `init()` + `SHUTDOWN` lifecycle hygiene.
  - Extracting reusable Phaser UI button primitives under client/src/ui/ if additional HUD controls are needed.
- Required dependencies and contracts:
  - Any new scene must receive `IGameNetworkService` via constructor (or Phaser scene data/registry typed wrapper) rather than importing global mutable state directly where avoidable, and must store unsubscribe functions returned by `on*` methods for `SHUTDOWN` disposal.
- Automated xUnit & client testing implications:
  - Run `npm --prefix client run build` and `powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1` after any client change, followed by manual two-tab/three-tab browser verification.

## Integration Notes

- Upstream dependencies:
  - `phaser` (`^4.2.1`), `IGameNetworkService` (client/src/services/GameNetworkService.ts), and DTO types (client/src/types/).
- Downstream consumers:
  - Browser entry point client/index.html loading client/src/main.ts inside `#game-container`.
- Cross-layer coupling risks:
  - `BoardScene.renderWinningLine` indexes `winningLine.cells[0]` and `winningLine.cells[2]` as line endpoints; server `WinningLineDto.Cells` must always order the three cells sequentially from start to end of the line (as `Board.EvaluateWin` currently guarantees).
  - When connection drops (`Disconnected` / `Reconnecting`), `BoardScene` clears `_latestState = null` without immediately clearing existing `_symbols` on the canvas until the next `applyGameState` arrives on rejoin.

## Known Risks and Open Questions

- Known limitations:
  - Fixed `800x600` aspect ratio scaled via `Phaser.Scale.FIT`; ultra-narrow mobile portrait viewports rely on letterboxed scaling rather than a responsive portrait layout reorganization.
  - No automated headless test suite currently exercises `BoardScene` or `BoardHud`.
- Open design questions:
  - Should board cells visually clear immediately when transitioning to `Disconnected` / `Reconnecting`, or remain visible as last-known board context until rejoin completes? Confirm with the user if UX changes are requested.
- Suggested follow-up tasks:
  - Perform live multi-tab browser checks for hover states, symbol/win-line tweens, rematch button transitions (`1/2 Ready` -> swapped symbols), and 3rd-tab `Retry Join` flow (currently unverified in this documentation pass).
