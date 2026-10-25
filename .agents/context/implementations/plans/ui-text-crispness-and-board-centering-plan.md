# Implementation Plan - UI Text Crispness and Board Centering

Date: 2026-10-25

Fix two visual presentation defects in the Phaser 4 client application:
1. The game board and HUD are shifted to the right of the viewport center due to conflicting horizontal centering between CSS Flexbox on `#game-container` and Phaser's `Phaser.Scale.CENTER_BOTH` inline `margin-left` calculation.
2. UI text and board symbols appear blurry on modern and high-DPI displays because the WebGL canvas renders at a low fixed `800x600` pixel buffer stretched via CSS (`Phaser.Scale.FIT`), combined with default `1x` text texture rasterization (`resolution: 1`) and sub-pixel vertex positioning.

## User Review Required

> [!IMPORTANT]
> This change preserves the existing `800x600` logical coordinate system used by `BoardScene` and `BoardHud` while scaling the underlying canvas resolution and text texture resolution by a high-DPI multiplier (`RENDER_RESOLUTION`) and removing the duplicate CSS Flexbox centering on `#game-container`. No server code, network protocol contracts, or gameplay rules are modified.

## Open Questions

- None.

## Proposed Changes

### Client / Phaser Scene / UI Area

#### [MODIFY] client/index.html
- Remove `display: flex`, `justify-content: center`, and `align-items: center` from `body` and `#game-container` so `#game-container` acts as a standard `100vw x 100vh` block container and `Phaser.Scale.CENTER_BOTH` centers the `<canvas>` element without double-offsetting `margin-left`.
- Add `display: block` on `#game-container canvas` to avoid inline baseline spacing.

#### [MODIFY] client/src/config/displayConfig.ts
- Create a shared client display configuration module exporting `LOGICAL_WIDTH` (`800`), `LOGICAL_HEIGHT` (`600`), and `RENDER_RESOLUTION` (clamped integer high-DPI multiplier derived from `window.devicePixelRatio`, minimum `2`, maximum `3`).

#### [MODIFY] client/src/main.ts
- Configure `Phaser.Game` with `width: LOGICAL_WIDTH * RENDER_RESOLUTION`, `height: LOGICAL_HEIGHT * RENDER_RESOLUTION`, `roundPixels: true`, and `scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH }`.

#### [MODIFY] client/src/scenes/BoardScene.ts
- Configure the main camera in `create()` with `setZoom(RENDER_RESOLUTION)`, `centerOn(LOGICAL_WIDTH / 2, LOGICAL_HEIGHT / 2)`, and `setRoundPixels(true)` so all scene objects continue using the `800x600` logical coordinate space while rendering into the high-resolution framebuffer.
- Set `resolution: RENDER_RESOLUTION` on board symbol `Phaser.GameObjects.Text` instances in `renderSymbolWithAnimation`.

#### [MODIFY] client/src/ui/BoardHud.ts
- Set `resolution: RENDER_RESOLUTION` and `setVertexRoundMode('fullAuto')` on all HUD `Phaser.GameObjects.Text` instances (`_connectionText`, `_symbolText`, `_statusText`, `_feedbackText`, `_rematchText`, `_retryText`) so text textures rasterize at high DPI and snap to integer framebuffer pixel coordinates.
- Align `SYMBOL_BADGE_X` to `675` so both top-bar pills share a symmetric `30px` outer margin relative to the `800px` logical width.

---

## Verification Plan

### Automated Checks
- Server compilation & xUnit tests:
```powershell
dotnet build
dotnet test
```
- Client TypeScript typecheck & production build:
```powershell
npm --prefix client run build
```
- Architecture & coding standards linter:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

### Manual Verification
1. Open the client in a desktop browser window (16:9 or ultrawide) and verify that the 3x3 board, status banners, and top HUD bar are horizontally centered in the viewport.
2. Verify that all HUD texts (`Connected (LAN)`, symbol badge, turn/status banner, action buttons) and board symbols (`X`, `O`) render with sharp edges without blurring.
3. Verify that pointer hover highlights and click zones on all 9 board cells and HUD action buttons align accurately with their visual rectangles.
