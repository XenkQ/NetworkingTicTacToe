# Implementation Plan - Core Architecture Decision Records (ADRs 0001-0005)

Date: 2026-10-25

Create the foundational set of five implementation-grounded Architecture Decision Records under .agents/context/adr/ documenting the settled architectural choices, rejected alternatives, trade-offs, and agent guardrails of the Networking Tic-Tac-Toe MVP, and reflect the populated ADR directory in README.md.

## User Review Required

> [!IMPORTANT]
> Approved 5-ADR scope: create ADRs 0001 through 0005 covering authoritative server and domain isolation, SignalR WebSocket JSON snapshots with monotonic versioning, single-session in-memory concurrency and seat lifecycle, Phaser 4 canvas UI with decoupled SignalR transport, and self-contained LAN static hosting with Vite dev proxying. Optional meta-ADR 0006 is intentionally omitted per user instruction. Runtime code and tests remain untouched.

## Open Questions

- None. The 5-document ADR scope was reviewed and confirmed by the user.

## Proposed Changes

### Architecture Decision Records (.agents/context/adr/)

#### [NEW] .agents/context/adr/0001-authoritative-server-and-pure-domain-isolation.md
- Document the decision to enforce 100% server authority (zero optimistic client board mutation) and strict isolation of pure C# game rules in server/Domain/ from SignalR/ASP.NET Core transport (`CS-ARCH005`), enabling fast deterministic xUnit verification in tests/Domain/.

#### [NEW] .agents/context/adr/0002-signalr-websocket-json-and-monotonic-state-snapshots.md
- Document the selection of SignalR over WebSockets with JSON serialization to satisfy browser TCP/JSON networking requirements, alongside full atomic `GameStateDto` broadcasts guarded by monotonic `StateVersion` and independent assignment versioning over delta-event streams.

#### [NEW] .agents/context/adr/0003-in-memory-single-session-and-seat-lifecycle-policy.md
- Document the MVP state model in server/Services/GameSessionService.cs (`IGameSessionService` singleton synchronized with `Lock`), covering the single active 2-player match, 3rd-caller `Spectator (Room Busy)` snapshot fallback with manual `Retry Join`, mutual rematch `X`/`O` seat swapping, and immediate post-disconnect promotion of the remaining participant to waiting Player `X`.

#### [NEW] .agents/context/adr/0004-phaser-canvas-ui-and-decoupled-network-service.md
- Document the decision to render both the 3x3 board and HUD inside Phaser 4 Canvas/WebGL with unified high-DPI scaling (`LOGICAL_WIDTH`, `LOGICAL_HEIGHT`, `RENDER_RESOLUTION` in client/src/config/displayConfig.ts) while isolating `@microsoft/signalr` inside client/src/services/GameNetworkService.ts (`TS-ARCH003`) with mandatory `SHUTDOWN` listener cleanup.

#### [NEW] .agents/context/adr/0005-self-contained-lan-hosting-and-dual-dev-mode.md
- Document the dual runtime hosting model in server/Program.cs, server/NetworkingTicTacToe.Server.csproj, and client/vite.config.ts: Kestrel binding on `http://0.0.0.0:5079` serving built static assets from client/dist/ (or published server/wwwroot/) with RFC1918 private-LAN CORS filtering, paired with Vite `:5173` WebSocket proxying during frontend development.

---

### Root Repository Documentation

#### [MODIFY] README.md
- Expand the .agents/context/adr/ node in the AI-Native workspace structure tree so readers can see the five canonical ADR files alongside the four game-system specifications.

---

## Verification Plan

### Automated Checks
- Verify that all file and directory paths inside .agents/ markdown documents use plain-text relative repository paths without markdown links or backticks, and strictly adhere to the English Language Invariant.
- Run architecture and coding standards linter:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

### Manual Verification
1. Review markdown rendering and tree alignment in README.md and .agents/context/adr/. Runtime browser behavior is unchanged in this documentation-only pass.
