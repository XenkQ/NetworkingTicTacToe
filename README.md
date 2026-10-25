# Networking Tic-Tac-Toe

A 2-player networked turn-based Tic-Tac-Toe web game built with an authoritative server architecture.

This project is a personal mini-project designed to validate initial project bootstrapping and configuration skills, with a strong focus on **AI-native agentic workflows** and the rapid end-to-end delivery of a simple game featuring a real-time networking layer.

## Demo

https://github.com/user-attachments/assets/3e5970f8-0415-4fc1-8b2b-dfb89064441f



## Purpose & Key Features

- **Authoritative Server Architecture**: The server owns the canonical 3x3 board state, turn order, move legality, win/draw evaluation, and session lifecycle. Clients render authoritative snapshots and dispatch player intent over SignalR WebSockets.
- **Automatic Session Pairing**: The first connected client claims **Player X** and waits for an opponent; the second client claims **Player O** and immediately starts the match.
- **Real-Time Move Validation & Visual Feedback**: Legal moves broadcast monotonic state snapshots (`StateVersion`) to participants with animated symbol placement and winning-line highlights. Illegal moves (out of turn, occupied cell, out of bounds, inactive game) are deterministically rejected.
- **Mutual Rematch with Symbol Swap**: After a win or draw, both active players can vote for a rematch (`1/2 Ready` -> `2/2 Ready`), resetting the board and swapping `X` and `O` assignments for the next round.
- **Disconnect Recovery & Promotion**: If an active player disconnects mid-match or during post-game, the match resets cleanly and the remaining player is promoted to waiting **Player X** for the next joiner.
- **Spectator / Room-Busy Handling (3rd+ Client)**: Additional clients connecting while a 2-player session is full enter read-only `Spectator (Room Busy)` mode with a **Retry Join** action to refresh the board snapshot or claim a seat once a player leaves.
- **LAN & Local Multi-Client Ready**: Configured to bind on `http://0.0.0.0:5079` by default with private LAN CORS support and static asset hosting from `client/dist`.

## Technology Stack

- **Client**: Phaser 4 (`^4.2.1`), TypeScript (`^5.7.3`, `strict: true`), Vite (`^6.2.0`), `@microsoft/signalr` (`^10.0.11`)
- **Server**: C# (.NET 10), ASP.NET Core, SignalR WebSockets, built-in `IServiceCollection` Dependency Injection
- **Testing**: xUnit on .NET 10 (`tests/`) covering domain rules, session concurrency, and hub orchestration

## Project Structure

```text
NetworkingTicTacToe/
├── client/                         # Phaser 4 + TypeScript frontend SPA
│   ├── src/
│   │   ├── main.ts                 # Phaser game bootstrap & scene registration
│   │   ├── scenes/
│   │   │   └── BoardScene.ts       # Interactive 3x3 grid, tweens, and network event wiring
│   │   ├── services/
│   │   │   └── GameNetworkService.ts # SignalR connection, auto-reconnect, and RPC client
│   │   ├── types/
│   │   │   ├── GameDomainTypes.ts      # Literal union types (symbols, status, errors, connection)
│   │   │   ├── GameStateDtos.ts        # BoardCellCoordinateDto, WinningLineDto, GameStateDto
│   │   │   ├── JoinGameResponseDto.ts  # JoinGame RPC response contract
│   │   │   ├── MoveDtos.ts             # MakeMoveRequestDto & MoveResultDto contracts
│   │   │   ├── PlayerAssignmentDto.ts  # OnPlayerAssignmentChanged event contract
│   │   │   ├── RematchResponseDto.ts   # RequestRematch RPC response contract
│   │   │   └── index.ts                # Barrel export for client types and DTOs
│   │   └── ui/
│   │       └── BoardHud.ts         # Connection status pill, symbol badge, banners, and buttons
│   ├── index.html
│   ├── package.json
│   ├── tsconfig.json
│   └── vite.config.ts              # Dev server (:5173) with WebSocket proxy to backend (:5079)
├── server/                         # ASP.NET Core 10 + SignalR authoritative server
│   ├── Domain/
│   │   ├── Board/                  # Pure 3x3 board state, coordinates, and winning line detection
│   │   ├── Match/                  # GameMatch state machine, turn rules, and error codes
│   │   └── Results/                # Immutable domain operation result records
│   ├── DTOs/
│   │   ├── GameStateDtos.cs        # BoardCellCoordinateDto, WinningLineDto, GameStateDto
│   │   ├── JoinGameResponseDto.cs  # JoinGame RPC response record
│   │   ├── MoveDtos.cs             # MakeMoveRequestDto & MoveResultDto records
│   │   ├── PlayerAssignmentDto.cs  # OnPlayerAssignmentChanged event record
│   │   └── RematchResponseDto.cs   # RequestRematch RPC response record
│   ├── Hubs/
│   │   └── GameHub.cs              # Strongly-typed SignalR hub (/hubs/game)
│   ├── Services/
│   │   └── GameSessionService.cs   # Thread-safe singleton session coordinator
│   └── Program.cs                  # DI wiring, LAN CORS policy, static files, and /health endpoint
├── tests/                          # xUnit automated test suite (.NET 10)
│   ├── Domain/                     # BoardTests & GameMatchTests
│   ├── Hubs/                       # GameHubTests
│   └── Services/                   # GameSessionServiceTests
├── .agents/                        # AI agent operational context, coding standards, and skills
├── AGENTS.md                       # Vendor-neutral agent entrypoint and workflow guide
└── GEMINI.md                       # Gemini / Antigravity runtime adapter directives
```

## Local Multi-Client Testing (1 Server + 2 or 3 Clients)

You can test the full multiplayer and spectator flows locally on a single machine using **one server instance** and **2 or 3 browser tabs/windows** (Use **2 clients** for head-to-head gameplay, or **3 clients** if you also want to test the spectator / room-full experience).

### Step 1: Start the Server and Client

Choose either **Option A** (Vite dev server with hot reload) or **Option B** (single-server static hosting):

#### Option A: Development Mode (Vite Dev Server + ASP.NET Core Backend)

1. **Terminal 1 — Start the authoritative C# server** (listens on `http://localhost:5079`):
   ```powershell
   dotnet run --project server
   ```
2. **Terminal 2 — Start the Vite client dev server** (listens on `http://localhost:5173` and proxies `/hubs` and `/health` to `:5079`):
   ```powershell
   npm --prefix client install
   npm --prefix client run dev
   ```
3. Open your browser tabs at **`http://localhost:5173`**.

#### Option B: Single-Port Hosting (Server Serving Built Client Assets)

1. **Build the client bundle into `client/dist`**:
   ```powershell
   npm --prefix client install
   npm --prefix client run build
   ```
2. **Start the C# server** (automatically serves `client/dist` at root `/`):
   ```powershell
   dotnet run --project server
   ```
3. Open your browser tabs at **`http://localhost:5079`** (or `http://<your-lan-ip>:5079` from other devices on the same network).

---

### Step 2: Test with 2 Clients (Active Match Flow)

1. **Open Client 1 (Browser Tab 1)**:
   - Connects to `/hubs/game` and invokes `JoinGame`.
   - Badge displays **`Your Symbol: X`** and banner shows **`Waiting for opponent...`**.
2. **Open Client 2 (Browser Tab 2)**:
   - Joins as **`Your Symbol: O`**.
   - The match starts immediately (`InProgress`): Client 1 sees **`Your Turn (X)`**, and Client 2 sees **`Opponent's Turn (X)`**.
3. **Play a Match**:
   - Alternate moves between Tab 1 (`X`) and Tab 2 (`O`).
   - Verify that clicking out of turn or on an occupied cell shows an immediate rejection message without corrupting the board.
4. **Test Rematch & Symbol Swap**:
   - Complete the game (Win or Draw). Both tabs display the **`Accept Rematch`** button.
   - Click **`Accept Rematch`** in Tab 1 (button updates to `Waiting for Opponent (1/2)...` and Tab 2 shows `Accept Rematch (1/2 Ready)`).
   - Click **`Accept Rematch`** in Tab 2 — the board clears, a new match starts, and player symbols swap (**Tab 1 becomes `O`**, **Tab 2 becomes `X`**).

---

### Step 3: Test with 3 Clients (Spectator / Room-Full & Seat Takeover Flow)

While **Client 1 (`X`)** and **Client 2 (`O`)** are connected in an active match:

1. **Open Client 3 (Browser Tab 3)**:
   - Because both player slots (`2/2`) are occupied, the server returns `IsJoined: false` with the current authoritative board snapshot.
   - Tab 3 enters spectator mode: the badge displays **`Spectator (Room Busy)`**, the status banner shows **`Game is currently full (2/2 players active)`**, and a **`Retry Join`** button appears at the bottom.
2. **Verify Spectator Restrictions & Snapshot Refresh**:
   - Click any cell on the board in Tab 3 — the client blocks the move with feedback: `Cannot make moves: Game is currently full (2/2 players active).`
   - Make a move in Tab 1 or Tab 2, then click **`Retry Join`** in Tab 3 to pull the latest board snapshot while remaining in spectator mode.
3. **Test Disconnect & Spectator Seat Takeover**:
   - Close **Tab 2** (or **Tab 1**).
   - The remaining active tab is automatically promoted to **`Your Symbol: X`** with status **`Opponent disconnected. Waiting for a new opponent...`**.
   - In **Tab 3**, click **`Retry Join`** — Tab 3 now claims the open **`Your Symbol: O`** slot and a fresh 2-player match begins immediately!

## Development & Validation

- **Server Build & Unit Tests**:
  ```powershell
  dotnet build
  dotnet test
  ```
- **Client Typecheck & Production Build**:
  ```powershell
  npm --prefix client run build
  ```
- **Architecture & Coding Standards Linter**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
  ```

## AI-Native Architecture & Agentic Workflow

Beyond delivering a networked multiplayer game, this repository is structured from day one as an **AI-native engineering workspace**. Instead of relying on ad-hoc chat prompts or ephemeral IDE context, AI coding agents operate inside a version-controlled harness with explicit entrypoints, modular context routing, reusable workflow skills, automated architectural linters, and an auditable in-repository delivery lifecycle.

### Human-in-the-Loop Ownership vs. Agent Responsibilities

The project enforces a strict collaboration boundary where the developer retains product and architectural authority while delegating scoped, verifiable engineering execution to the agent:

- **Human Engineer (Product & Architecture Owner)**:
  - Defines gameplay scope, player-facing rules, matchmaking/spectator policies, and visual/UX direction.
  - Resolves open design trade-offs at explicit specification gates and approves implementation plans before code changes begin.
  - Performs final interactive acceptance testing across live multi-client browser sessions and LAN devices.
- **AI Agent (Scoped Execution & Verification Partner)**:
  - Grounds all reasoning strictly in live source code and `.agents/` operational files rather than assumptions, never silently altering game rules or network contracts.
  - Preserves architectural invariants across both stacks: 100% server authority, pure framework-free domain logic (`server/Domain/`), synchronized modular DTOs (`server/DTOs/` <-> `client/src/types/`), strict TypeScript (`strict: true`, zero `any`), and deterministic Phaser `SHUTDOWN` resource cleanup.
  - Maintains living technical specifications, writes deterministic xUnit unit/concurrency tests, and runs multi-layer build and architecture gates before completing any task.

### Agentic Workspace Structure (`.agents/` & Entrypoints)

To avoid monolithic instruction bloat and vendor lock-in, agent guidance is split into a canonical vendor-neutral router (`AGENTS.md`), thin tool-specific adapters (`GEMINI.md` and optional `openai.yaml` skill descriptors), and a modular `.agents/` directory loaded on demand based on task scope:

```text
NetworkingTicTacToe/
├── AGENTS.md                             # Canonical vendor-neutral agent guide, rules, and workflow router
├── GEMINI.md                             # Runtime adapter enforcing in-repo precedence over IDE artifact folders
└── .agents/
    ├── context/                          # Modular operational knowledge base & lifecycle storage
    │   ├── adr/                          # Architecture Decision Records (ADRs 0001-0005)
    │   │   ├── 0001-authoritative-server-and-pure-domain-isolation.md
    │   │   ├── 0002-signalr-websocket-json-and-monotonic-state-snapshots.md
    │   │   ├── 0003-in-memory-single-session-and-seat-lifecycle-policy.md
    │   │   ├── 0004-phaser-canvas-ui-and-decoupled-network-service.md
    │   │   └── 0005-self-contained-lan-hosting-and-dual-dev-mode.md
    │   ├── brainstorming-summaries/      # Design exploration & trade-off briefs
    │   ├── game-systems/                 # Source-verified living system specs (*-system.md)
    │   │   ├── board-and-rules-system.md
    │   │   ├── matchmaking-and-session-system.md
    │   │   ├── phaser-board-ui-system.md
    │   │   └── signalr-network-protocol-system.md
    │   ├── implementations/              # Version-controlled task lifecycle artifacts
    │   │   ├── plans/                    # Pre-code technical specs & plans (*-plan.md / *-spec.md)
    │   │   ├── summaries/                # Post-implementation validation reports (*-summary.md)
    │   │   └── templates/                # Standardized plan & summary templates
    │   ├── tmp/                          # Git-ignored scratchpad & multi-agent handoff workspace
    │   ├── project-coding-standards.md   # Standards index routing to shared, server, and client modules
    │   ├── shared-coding-standards.md    # Cross-stack rules, DTO parity, and English language invariant
    │   ├── server-coding-standards.md    # C# / .NET 10, DI, SignalR, and xUnit conventions
    │   ├── client-coding-standards.md    # TypeScript strictness, Phaser 4 scene lifecycle, and service split
    │   ├── ai-game-dev-best-practices.md # Networked turn-based guardrails & concurrency failure modes
    │   └── technology-documentation.md   # Curated official docs index (.NET 10, SignalR, Phaser 4, Vite)
    ├── scripts/
    │   └── check-architecture.ps1        # Static architecture linter enforcing CS-ARCH and TS-ARCH rules
    └── skills/                           # 12 trigger-driven workflow skills (portable SKILL.md + templates)
```

### Reusable Agent Skills (`.agents/skills/`)

Specialized engineering workflows are encapsulated into **12 modular skills** (each authored as a portable `SKILL.md` playbook with companion templates/references and optional UI descriptors) triggered automatically when a task matches their domain:

| Responsibility Area | Skills | Role in the Workflow |
| :--- | :--- | :--- |
| **Discovery & Specification** | `game-brainstorm`, `gameplay-spec-writing` | Explore mechanic/UX trade-offs and author staff-level technical specifications with mandatory **Open Questions** gates prior to coding. |
| **Implementation & Standards** | `phaser-core`, `preserve-coding-standards` | Guide strict TypeScript Phaser 4 scene lifecycle/rendering patterns and remediate naming, member-ordering, or style drift incrementally. |
| **Diagnostics & Quality Gates** | `root-cause`, `architecture-review`, `batch-codebase-review`, `pre-commit-gate` | Perform read-only bug/race-condition tracing, audit DI and client-server ownership boundaries (single-scope or multi-agent batched), and run the 6-stage pre-commit verification gate. |
| **Living Documentation** | `document-system`, `batch-system-docs-update`, `agent-docs-review`, `create-user-doc` | Keep `.agents/context/game-systems/` and operational docs synchronized with live code, while isolating human-oriented docs in `.user-docs/` upon explicit request. |

### How the Agent Works: In-Repo Implementation Lifecycle

Every non-trivial feature, architectural change, or multi-file refactor follows a deterministic **(Brainstorm ->) Plan -> Execute -> Gate -> Summary** lifecycle tracked directly in Git:

1. **Targeted Context Loading**: The agent enters through `AGENTS.md`, routes via `project-coding-standards.md` to only the relevant stack rules (`shared`, `server`, or `client`), inspects impacted `game-systems/*-system.md` documents and source files, and loads any triggered `SKILL.md`.
2. **Phase 0 (Optional) — Read-Only Brainstorming (`game-brainstorm`)**: For open-ended mechanics, new game modes, UX flows, or architectural trade-offs, the agent operates under a strict read-only gate—evaluating at least two viable alternatives plus a minimal baseline across UX, server authority, concurrency, and xUnit testability. Once converged, it saves a durable handoff brief in `.agents/context/brainstorming-summaries/` to seed formal specification.
3. **Phase 1 — In-Repo Planning & User Confirmation Gate (`gameplay-spec-writing`)**: Before touching production code, the agent writes an implementation plan or spec in `.agents/context/implementations/plans/` (never solely in external IDE scratch/brain directories). If any gameplay rule, protocol contract, or UX detail is underspecified, execution stops at an explicit **Open Questions** gate for human confirmation.
4. **Phase 2 — Scoped Execution & Workspace Hygiene**: Once approved, the agent implements focused cross-stack changes without mixing unrelated refactors. All repository artifacts follow a strict **English Language Invariant** (while chat remains multilingual), and any temporary scratch scripts or multi-agent batch handoff files are strictly confined to `.agents/context/tmp/` to keep the Git worktree clean.
5. **Phase 3 — Automated Verification Gate (`pre-commit-gate`)**: Changes must pass a zero-warning multi-layer check before completion:
   - C# compilation (`dotnet build`) and deterministic xUnit suite (`dotnet test`).
   - Strict TypeScript typechecking and Vite production bundling (`npm --prefix client run build`).
   - Static architecture audit (`check-architecture.ps1`), blocking anti-patterns such as static mutable singletons (`CS-ARCH001`), SignalR transport leaks in `server/Domain/` (`CS-ARCH005`), explicit `any` or `@ts-ignore` (`TS-ARCH001`/`TS-ARCH002`), and direct `HubConnectionBuilder` usage inside Phaser scenes (`TS-ARCH003`).
6. **Phase 4 — In-Repo Summary & Audit Trail**: Upon passing automated gates, the agent records a permanent summary in `.agents/context/implementations/summaries/` detailing modified files, verification evidence, and remaining manual multi-tab browser checks—preserving a complete, version-controlled engineering history alongside the code.

