# Implementation Plan - Initial Game Systems Documentation

Date: 2026-10-25

Create the initial set of four implementation-grounded, agent-facing technical system documents under .agents/context/game-systems/ using the workflow and template defined in .agents/skills/document-system/SKILL.md and .agents/skills/agent-docs-review/SKILL.md.

## User Review Required

> [!IMPORTANT]
> Approved 4-system documentation architecture: separate canonical system documents for Board and Match Rules Domain, Matchmaking and Session Management, SignalR Protocol and Network Transport, and Phaser Client Scene and HUD UI. Runtime code and tests remain untouched.

## Open Questions

- None. The 4-system partition and execution scope were reviewed and confirmed by the user.

## Proposed Changes

### Agent System Documentation (.agents/context/game-systems/)

#### [NEW] .agents/context/game-systems/board-and-rules-system.md
- Document the pure C# domain layer in server/Domain/ (Board, BoardCoordinate, CellState, WinningLine, GameMatch, GameStatus, MoveErrorCode, PlayerSymbol, and domain result records), corresponding client union types in client/src/types/GameDomainTypes.ts, deterministic validation/evaluation order, rematch symbol swapping, disconnect promotion rules, and xUnit coverage in tests/Domain/.

#### [NEW] .agents/context/game-systems/matchmaking-and-session-system.md
- Document IGameSessionService and GameSessionService in server/Services/GameSessionService.cs, singleton DI registration in server/Program.cs, single-room 2-player seating vs 3rd-caller room-busy behavior, Lock synchronization, monotonic StateVersion rules, snapshot generation, and concurrency/unit test coverage in tests/Services/GameSessionServiceTests.cs.

#### [NEW] .agents/context/game-systems/signalr-network-protocol-system.md
- Document GameHub and IGameClient in server/Hubs/GameHub.cs, LAN CORS and static/proxy hosting in server/Program.cs and client/vite.config.ts, modular C# and TypeScript DTO contracts across server/DTOs/ and client/src/types/, client WebSocket transport and reconnect/version-guard mechanics in client/src/services/GameNetworkService.ts, and hub unit tests in tests/Hubs/GameHubTests.cs.

#### [NEW] .agents/context/game-systems/phaser-board-ui-system.md
- Document Phaser 4 game initialization in client/src/main.ts, 3x3 canvas grid rendering, tweens, input gating, and SHUTDOWN lifecycle cleanup in client/src/scenes/BoardScene.ts, and canvas HUD status pills, banners, feedback timers, and contextual action buttons in client/src/ui/BoardHud.ts.

---

## Verification Plan

### Automated Checks
- Verify relative plain-text file paths, cross-references, and English language invariant across all created markdown documents under .agents/context/.
- Run architecture & coding standards linter to confirm repository cleanliness:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

### Manual Verification
1. Live two-tab and three-tab browser checks remain unverified during this documentation-only task and are recorded explicitly as pending runtime checks where applicable.
