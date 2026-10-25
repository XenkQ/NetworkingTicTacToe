---
name: phaser-core
description: "Use when: setting up, structuring, or debugging Phaser 4 scenes, canvas scaling, lifecycle cleanup, or reactive client rendering in strict TypeScript. Triggers: phaser, Phaser.Game, Phaser.Scene, scene lifecycle, preload, create, update, shutdown, scene transition, canvas scaling, board UI rendering, input handling."
---

# Phaser 4 Core Client Skill

Set up, structure, and debug client-side Phaser 4 scenes and UI rendering for Networking Tic-Tac-Toe using strict TypeScript and Vite.

## When to Use

- Use when creating or modifying Phaser Scenes (`client/src/scenes/`), such as `BootScene`, `LobbyScene`, or `BoardScene`.
- Use when configuring `Phaser.Game`, canvas scaling, responsive viewports, and asset preloading.
- Use when designing interactive board cells, symbol animations (X and O placement tweens), and HUD/modal overlays.
- Use when fixing client scene lifecycle issues, stale state across rematches, or duplicate event listeners.

**When *not* to use:**
- Do not use for server-side game rules, win validation, or session logic (use pure domain logic under `server/Domain/` and xUnit tests).
- Do not use for physics simulations (velocity, gravity, colliders, Arcade/Matter physics are not needed for turn-based board games).
- Do not use for direct SignalR connection management or protocol contracts (delegate to `client/src/services/` and `server/DTOs/`).

---

## Architectural Guardrails for this Repository

Every Phaser scene in this project must adhere to the rules defined in `.agents/context/project-coding-standards.md`:

1. **Separation of Presentation and Networking**:
   - Phaser Scenes own visual `GameObjects`, grid layout, animations/tweens, and user input events.
   - Networking Services in `client/src/services/` own `@microsoft/signalr` `HubConnection`, reconnection policies, and strongly-typed event emissions.
   - **Strict Negative Constraint**: Never instantiate `HubConnectionBuilder` or establish WebSocket connections directly inside a Phaser Scene.
2. **Strict TypeScript (Zero `any`)**:
   - `strict: true` is strictly enforced. Never use `any` or `@ts-ignore`.
   - Define explicit TypeScript interfaces for all scene initialization payloads (e.g. `interface BoardSceneData`).
3. **Mandatory Lifecycle Cleanup**:
   - Any subscription to service events or global DOM listeners registered in `init()` or `create()` must be unregistered in `this.events.once(Phaser.Scenes.Events.SHUTDOWN, ...)`.
   - Stale tweens and timers must be killed on scene transition to avoid running callbacks on destroyed game objects.
4. **Authoritative Server Principle**:
   - The server owns canonical board state and turn order. The client scene renders state received from the server and dispatches player intent (e.g. `onCellClicked(row, col)`).
   - Disable input zones while awaiting server move confirmation or when it is the opponent's turn.

---

## Core Workflow

1. **Configure the Game**:
   Define `Phaser.Types.Core.GameConfig` with `type: Phaser.AUTO`, responsive scale mode (`Phaser.Scale.FIT`, `autoCenter: Phaser.Scale.CENTER_BOTH`), and registered scene classes.
2. **Model Each Screen as a Scene**:
   Subclass `Phaser.Scene`, pass a unique scene key to `super({ key: 'SceneKey' })`, and implement the lifecycle:
   `init(data)` -> `preload()` -> `create(data)` -> `update(time, delta)` (optional for turn-based games).
3. **Reset State in `init()`**:
   Never rely on class field initializers for per-match state. Scene instances are reused across restarts; always reset board arrays, player turn indicators, and match flags inside `init(data)`.
4. **Preload Assets in `preload()`**:
   Queue textures, audio, and spritesheets. Use them only after `create()` is called.
5. **Construct View & Wire Events in `create()`**:
   Build the 3x3 grid, create interactive click zones, render HUD text, and subscribe to strongly-typed network service events.
6. **Register Shutdown Teardown**:
   Unsubscribe all network and global listeners to prevent memory leaks and duplicate moves.

---

## Implementation Patterns

### 1. Game Configuration (`client/src/main.ts`)

```typescript
import Phaser from 'phaser';
import { BootScene } from './scenes/BootScene';
import { LobbyScene } from './scenes/LobbyScene';
import { BoardScene } from './scenes/BoardScene';

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  width: 800,
  height: 600,
  backgroundColor: '#1a1a24',
  parent: 'game-container',
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH
  },
  scene: [BootScene, LobbyScene, BoardScene]
};

export const game = new Phaser.Game(config);
```

### 2. Typed Board Scene with SignalR Service Integration

```typescript
import Phaser from 'phaser';
import type { IGameClientService } from '../services/IGameClientService';
import type { MoveResultDto, GameStateDto } from '../types';

export interface BoardSceneData {
  readonly sessionId: string;
  readonly localPlayerSymbol: 'X' | 'O';
  readonly initialGameState: GameStateDto;
}

export class BoardScene extends Phaser.Scene {
  private readonly _gameService: IGameClientService;
  private _sessionId!: string;
  private _localPlayerSymbol!: 'X' | 'O';
  private _isMyTurn = false;
  private _cellZones: Phaser.GameObjects.Zone[][] = [];

  constructor(gameService: IGameClientService) {
    super({ key: 'BoardScene' });
    this._gameService = gameService;
  }

  public init(data: BoardSceneData): void {
    // Reset per-game state for clean restarts and rematches
    this._sessionId = data.sessionId;
    this._localPlayerSymbol = data.localPlayerSymbol;
    this._isMyTurn = data.initialGameState.currentTurn === data.localPlayerSymbol;
  }

  public preload(): void {
    // Load symbols, grid textures, and sounds if not loaded in BootScene
  }

  public create(): void {
    this.createGrid();
    this.setupNetworkSubscriptions();
  }

  private createGrid(): void {
    const cellSize = 120;
    const startX = 280;
    const startY = 180;

    this._cellZones = [];

    for (let row = 0; row < 3; row++) {
      this._cellZones[row] = [];
      for (let col = 0; col < 3; col++) {
        const x = startX + col * cellSize;
        const y = startY + row * cellSize;

        // Visual cell background / border
        this.add.rectangle(x, y, cellSize - 8, cellSize - 8, 0x2e2e3e);

        // Interactive click zone
        const zone = this.add.zone(x, y, cellSize, cellSize).setInteractive({ useHandCursor: true });
        zone.on(Phaser.Input.Events.POINTER_DOWN, () => {
          this.handleCellClick(row, col);
        });

        this._cellZones[row][col] = zone;
      }
    }
  }

  private handleCellClick(row: number, col: number): void {
    if (!this._isMyTurn) {
      return;
    }

    // Disable cell immediately to prevent duplicate clicks while awaiting server ack
    this._cellZones[row][col].disableInteractive();

    // Dispatch player intent to authoritative server via service
    this._gameService.makeMove(this._sessionId, row, col).catch((error: unknown) => {
      // Re-enable on failure
      this._cellZones[row][col].setInteractive({ useHandCursor: true });
      console.error('Failed to make move', error);
    });
  }

  private setupNetworkSubscriptions(): void {
    const onMoveMade = (move: MoveResultDto): void => {
      this.renderMove(move);
      this._isMyTurn = move.nextTurn === this._localPlayerSymbol;
    };

    this._gameService.on('MoveMade', onMoveMade);

    // CRITICAL: Unsubscribe on scene shutdown to prevent listener leaks across rematches
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
      this._gameService.off('MoveMade', onMoveMade);
      this.tweens.killAll();
      this.time.removeAllEvents();
    });
  }

  private renderMove(move: MoveResultDto): void {
    const cellSize = 120;
    const x = 280 + move.col * cellSize;
    const y = 180 + move.row * cellSize;

    const symbolText = this.add.text(x, y, move.symbol, {
      fontSize: '64px',
      color: move.symbol === 'X' ? '#ff4d4d' : '#4da6ff',
      fontStyle: 'bold'
    }).setOrigin(0.5).setScale(0);

    // Smooth placement tween animation
    this.tweens.add({
      targets: symbolText,
      scale: 1,
      duration: 200,
      ease: 'Back.easeOut'
    });
  }
}
```

---

## Common Pitfalls & Guardrails

- **Stale State on Rematch**: Fields set in the constructor or class field initializers do not reset on restart. Reset all match-specific flags and arrays in `init(data)`.
- **Double Move Submissions**: Always disable cell input immediately upon click and re-enable only if the server returns an error or rejects the move.
- **Zombie Event Listeners**: Always unregister service listeners (`this._gameService.off(...)`) inside `this.events.once(Phaser.Scenes.Events.SHUTDOWN, ...)`.
- **Unmanaged Hot-Reloading / Multiple Canvases**: Ensure Vite hot reload or page navigation cleans up the game instance using `game.destroy(true)` when tearing down the DOM container.
- **Phaser 3 vs Phaser 4 APIs**: Phaser 4 refactored rendering pipelines and masks into filters (`obj.enableFilters()`). Do not blindly copy legacy Phaser 3 custom shader pipeline snippets.

---

## References & Related Skills

- Detailed scene transitions, restart bugs, and state machines: read `references/scene-flow.md`.
- Project coding standards: `.agents/context/project-coding-standards.md`.
- Architecture review skill: `.agents/skills/architecture-review/SKILL.md`.
- Bug investigation skill: `.agents/skills/root-cause/SKILL.md`.
