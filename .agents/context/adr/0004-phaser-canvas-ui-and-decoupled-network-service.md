# ADR 0004: Phaser 4 Canvas UI and Decoupled Network Service

- **Status:** Accepted
- **Date:** 2026-10-25
- **Use when:** Modifying Phaser scene architecture, HUD rendering, viewport/DPI scaling, or client-side SignalR event wiring.

## Context

The browser client must render an interactive 3x3 board with responsive hover/placement tweens, winning-line highlights, connection telemetry, assigned player badges, turn/outcome banners, and contextual `Accept Rematch` / `Retry Join` actions across diverse desktop resolutions and high-DPI displays—while surviving WebSocket reconnects and Phaser scene restarts without ghost callbacks or memory leaks.

## Decision

1. **Unified Phaser 4 Canvas Rendering (Board + HUD):**
   - Render both the interactive 3x3 board (`BoardScene`) and all HUD controls/banners (`BoardHud`) inside the Phaser 4 WebGL/Canvas viewport rather than layering an external HTML/DOM or React UI framework on top of the canvas.
   - Standardize layout around a logical `800 x 600` coordinate space (`LOGICAL_WIDTH`, `LOGICAL_HEIGHT`) scaled by a clamped high-DPI multiplier (`RENDER_RESOLUTION` in `[2, 3]`) configured in client/src/config/displayConfig.ts, using `Phaser.Scale.FIT`, `Phaser.Scale.CENTER_BOTH`, `roundPixels: true`, and `cameras.main.setZoom(RENDER_RESOLUTION)` so text and vector shapes remain crisp across displays.
2. **Strict Separation Between Presentation and Transport:**
   - Encapsulate `HubConnectionBuilder`, WebSocket lifecycle, reconnect backoff, and RPC invocations inside `GameNetworkService` (`IGameNetworkService`) in client/src/services/GameNetworkService.ts, injected into `BoardScene` via its constructor in client/src/main.ts.
   - Architecture linter rule `TS-ARCH003` statically forbids instantiating `HubConnectionBuilder` inside any file under client/src/scenes/.
3. **Deterministic Scene `SHUTDOWN` Cleanup:**
   - Every listener registered on `IGameNetworkService` during `BoardScene.create()` returns or pairs with an explicit unsubscribe callback invoked on `Phaser.Scenes.Events.SHUTDOWN`, alongside `tweens.killAll()`, HUD timer disposal (`BoardHud.destroy()`), and per-session state resets in `init()`.

## Alternatives Considered

- **DOM / CSS / React Overlay HUD Over Phaser Canvas:** Rejected. Maintaining two separate layout/scaling engines (CSS media queries vs. `Phaser.Scale.FIT`) for a compact single-screen game introduces alignment drift, z-index pointer capture quirks, and unnecessary framework bundle weight.
- **Managing SignalR Directly Inside `BoardScene`:** Rejected. Tying WebSocket connection lifetime to a Phaser scene causes connection drops or duplicate event handlers whenever scenes restart or transition.

## Consequences and Guardrails

- **Positive:**
  - Single deterministic coordinate system (`800 x 600` logical units) with crisp high-DPI text rendering and zero DOM/canvas synchronization bugs.
  - `IGameNetworkService` can be reused across future scenes (such as a lobby or title scene) without reconnecting WebSockets.
- **Trade-offs & Invariants:**
  - All client code must compile under `strict: true` with zero explicit `any` (`TS-ARCH001`) and zero `@ts-ignore` / `@ts-nocheck` directives (`TS-ARCH002`).
  - Any new network event, Phaser timer, or input handler added to a scene or HUD helper must have a corresponding teardown path in `SHUTDOWN` / `destroy()`.

## Authoritative Files and Related Docs

- Client bootstrap & scaling: client/src/main.ts, client/src/config/displayConfig.ts, client/index.html
- Scene & HUD presentation: client/src/scenes/BoardScene.ts, client/src/ui/BoardHud.ts
- Decoupled network service: client/src/services/GameNetworkService.ts
- Automated enforcement: .agents/scripts/check-architecture.ps1, client/tsconfig.json
- Related system doc: .agents/context/game-systems/phaser-board-ui-system.md
