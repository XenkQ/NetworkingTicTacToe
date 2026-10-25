# Phaser Scene Flow & Lifecycle

The Scene Manager runs every scene through a small state machine. Knowing which transition verb does what prevents the most common Phaser bugs: hidden scenes, stale state after a restart, and duplicate event listeners.

## Lifecycle Order

A scene is **booted once** (status `INIT`) and can be **started any number of times**. On each start it runs:

```
start -> (loading, if preload queued anything) -> create -> running
```

- `init(data: T)` - runs first on every start. **Reset run-specific state here.**
- `preload()` - queue asset downloads. Optional.
- `create(data: T)` - build game objects; assets queued in `preload` are now ready.
- `update(time, delta)` - called every frame while the scene is `running` (often unused or minimal for turn-based board games).

The same strongly-typed `data` object is passed to `init` and `create`, coming from `scene.start(key, data)` or `scene.launch(key, data)`.

## Transition Verbs

| Method | Effect on target | Effect on caller | Typical Tic-Tac-Toe Use Case |
|---|---|---|---|
| `start(key, data)` | start (or restart) | **stop** the caller | Transitioning from LobbyScene to BoardScene |
| `launch(key, data)` | start in parallel; **restarts** if already running | caller keeps running | Launching an overlay HUD or Game Over modal |
| `run(key, data)` | wake if sleeping, resume if paused, otherwise (re)start | caller keeps running | Resuming a background scene |
| `switch(key)` | wake if sleeping, otherwise start | **sleep** the caller | Switching back to Matchmaking without destroying board |
| `pause(key?)` | stop updating, keep rendering | - | Pausing board interaction while settings menu is open |
| `resume(key?)` | resume updating | - | Closing settings menu |
| `sleep(key?)` | stop updating AND rendering, keep state | - | Minimizing lobby while board is active |
| `wake(key?)` | resume a sleeping scene | - | Re-activating lobby after match ends |
| `stop(key?)` | shutdown (frees game objects and listeners) | - | Explicit cleanup |

All transitions are queued: the Scene Manager applies them at the start of the next game step, so the target scene has not started yet on the line immediately following `start`/`launch`.

## The Restart-State Bug

A Scene instance is long-lived. Fields initialized in the class declaration or constructor run **once**, so they keep stale values across rematch restarts:

```typescript
// WRONG - isGameOver stays true forever after the first match.
export class BoardScene extends Phaser.Scene {
  private _isGameOver = false; // Initialized once at instantiation!
  
  constructor() {
    super('BoardScene');
  }
}

// RIGHT - init() runs on every restart.
export class BoardScene extends Phaser.Scene {
  private _isGameOver = false;

  constructor() {
    super('BoardScene');
  }

  public init(data: BoardSceneData): void {
    this._isGameOver = false; // Reset clean for every new game or rematch!
  }
}
```

## Preventing Duplicate Listeners & Event Leaks

Listeners added in `create()` on network services or global emitters survive scene restarts and stack up. In this project, scenes **must** unsubscribe from external and service emitters on `SHUTDOWN`:

```typescript
public create(): void {
  // Bind network service events
  const onMoveReceived = (dto: MoveResultDto): void => {
    this.handleMove(dto);
  };
  
  this._networkService.on('MoveMade', onMoveReceived);

  // Clean up when the scene stops so next run does not duplicate listeners
  this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => {
    this._networkService.off('MoveMade', onMoveReceived);
    this.tweens.killAll();
    this.time.removeAllEvents();
  });
}
```

## Modal & Overlay Ordering

- Overlay scenes (like `GameOverModal` or `RematchDialog`) launched via `this.scene.launch('GameOverModal', data)` must render on top of the board.
- Scenes render in scene-list configuration order. If an overlay scene is configured before `BoardScene` in `main.ts`, call `this.scene.bringToTop('GameOverModal')` immediately after launching.
