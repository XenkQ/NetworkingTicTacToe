# Client Coding Standards (TypeScript / Phaser 4 / Vite / SignalR)

## Purpose

This document defines the TypeScript type-safety rules, naming conventions, Phaser 4 scene lifecycle practices, and `@microsoft/signalr` client integration standards for the browser client application (client/).

Use this document alongside:
- .agents/context/shared-coding-standards.md for cross-stack architectural rules, English Language Invariant, and DTO contract alignment.
- .agents/context/project-coding-standards.md for the top-level standards index.

## Scope

Apply these rules to all TypeScript and configuration files under:
- client/src/ (main.ts, scenes/, services/, ui/, types/, models/)
- client/tsconfig.json and client/vite.config.ts

---

## 1) Strict TypeScript Type Safety

- **Strict Compiler Mode**: Always maintain `"strict": true` in client/tsconfig.json.
- **Ban on `any`**: Never use explicit `any` types (`: any`, `<any>`, `as any`). Define explicit `interface` or `type` declarations for all data structures, SignalR payloads, event callbacks, and scene parameters (enforced by rule `TS-ARCH001` in .agents/scripts/check-architecture.ps1). Use `unknown` with type guards when handling untyped external data.
- **Ban on Error Suppression**: Never use `@ts-ignore`, `@ts-nocheck`, or `@ts-expect-error` to bypass compiler errors (`TS-ARCH002`). Fix the underlying type mismatch instead.

---

## 2) TypeScript Naming Conventions

- **Classes, Interfaces, Types, Enums**: `PascalCase` (`BoardScene`, `NetworkService`, `GameStateDto`).
  - Note: TypeScript interfaces do **not** use an `I` prefix unless representing a behavioral service contract mirroring C#; data DTO interfaces use clean `PascalCase` (`MoveResultDto`, `RoomState`).
- **Variables, Parameters, Methods, Functions, Properties**: `camelCase` (`currentTurn`, `handleCellClick`, `connectToServer`).
- **Private Class Fields**: `private _camelCase` or standard `private camelCase` (stay consistent within each class; `_camelCase` is preferred for internal mutable state backing properties).
- **Constants**: `UPPER_SNAKE_CASE` (`CELL_SIZE`, `GRID_OFFSET_X`, `HUB_ENDPOINT`).
- **Filenames**:
  - Phaser scenes and classes: `PascalCase.ts` (`BoardScene.ts`, `MatchmakingScene.ts`) or `kebab-case.ts` matching the folder convention consistently.

---

## 3) Phaser 4 & SignalR Separation of Concerns

- **Scenes Own Presentation & Input**:
  - Phaser Scenes in client/src/scenes/ own visual GameObjects, layout positioning, animations/tweens, and pointer/keyboard input events.
  - Scenes do not manage raw WebSocket/SignalR connections or rule evaluation.
- **Services Own Networking**:
  - Networking services in client/src/services/ own the `@microsoft/signalr` `HubConnection`, connection lifecycle, reconnection policies, and strongly typed event emission.
  - Never instantiate `HubConnectionBuilder` directly inside a Phaser Scene (`TS-ARCH003`).

---

## 4) Phaser 4 Scene Lifecycle & Mandatory Cleanup

- **Reset State in `init()`**:
  - Phaser reuses Scene instances across restarts (`this.scene.restart()` or `this.scene.start()`). Reset per-match mutable state (board arrays, active turn flags, UI references, over/rematch flags) inside `init()` or `create()` rather than relying solely on class field initializers.
- **Mandatory Listener Unsubscription**:
  - Whenever a Phaser Scene subscribes to network service events, custom event emitters, or global DOM/Phaser events in `init()` or `create()`, it **must** unsubscribe in the scene's `SHUTDOWN` / `DESTROY` lifecycle handler:
    `this.events.once(Phaser.Scenes.Events.SHUTDOWN, () => { ... });`
- **Tween & Timer Cleanup**:
  - Clean up active Phaser tweens or delayed timers on scene shutdown/transition to prevent callbacks firing on destroyed GameObjects.

---

## 5) SignalR Client Resilience & Contract Alignment

- **Typed Service Wrappers**: Wrap `connection.invoke(...)` and `connection.on(...)` inside typed methods in client/src/services/ so scenes interact only with typed domain callbacks and promises.
- **DTO Synchronization & Modular Organization**: Keep TypeScript DTO interfaces in client/src/types/ (or client/src/models/) aligned with C# records in server/DTOs/, using `camelCase` property names matching ASP.NET Core SignalR's default JSON serialization. Mirror the server's modular file division: group related request/response or composite snapshot interfaces (e.g., client/src/types/MoveDtos.ts, client/src/types/GameStateDtos.ts), place standalone DTOs in their own files (e.g., client/src/types/JoinGameResponseDto.ts), keep shared literal union types in client/src/types/GameDomainTypes.ts, and re-export through client/src/types/index.ts rather than using a single monolithic contracts file.
- **Connection State Handling**: Handle connection drops, reconnection transitions (`withAutomaticReconnect`), and opponent disconnect notifications gracefully in the UI without leaving scenes in an unresponsive state.

---

## 6) Client Agent Execution Checklist

Before finalizing any TypeScript or Phaser 4 client change:

1. `npm --prefix client run build` succeeds with 0 TypeScript (`tsc`) or Vite bundling errors.
2. .agents/scripts/check-architecture.ps1 passes with 0 TypeScript violations (`TS-ARCH001` through `TS-ARCH003`).
3. Code contains zero `any` types and zero `@ts-ignore` / `@ts-nocheck` directives.
4. No `HubConnectionBuilder` usage exists inside client/src/scenes/.
5. All network and input listeners registered by a Phaser Scene are cleaned up on `Phaser.Scenes.Events.SHUTDOWN`.
6. Per-match scene state is properly reset in `init()` or `create()`.
7. Client DTO interfaces match server DTO records in property names, casing, and types.
