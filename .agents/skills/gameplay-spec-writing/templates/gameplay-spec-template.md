# Specification: [Feature Name]

**Date:** [YYYY-MM-DD]  
**Author:** [Agent / Author]  
**Target Systems:** [e.g. server/Domain/, server/Hubs/, client/src/scenes/, tests/]  

---

## 1. Overview & Player Experience
- **Summary:** Concise description of the gameplay or networking feature.
- **Player-Facing Goals:** How does this feel to the player? What visual/UI feedback is expected in Phaser?
- **In-Scope vs. Out-of-Scope:** Explicit scope boundaries.

---

## 2. Open Questions & Resolved Decisions
### Resolved Decisions
- Decision 1: [Confirmed choice and source of approval]
- Decision 2: [Confirmed choice and source of approval]

### Open Questions (Consequential Unknowns Only)

Reuse resolved choices. State None when no consequential unknown remains; routine reversible implementation choices do not require repeated approval. Save the skeleton at its canonical plan path before asking questions. Runtime implementation still follows the requirements gate in AGENTS.md.
- [ ] **Q1:** ...
- [ ] **Q2:** ...

---

## 3. Domain Model & Network Contracts
- **Server Domain Models (`server/Domain/`):**
  - Pure classes, state machines, and validation rules:
- **SignalR DTO Contracts (`server/DTOs/` & `client/src/types/`):**
  - C# `record` definitions and matching TypeScript `interface` definitions:
- **SignalR Hub RPCs & Broadcast Events (`server/Hubs/`):**
  - Client-to-Server invocations:
  - Server-to-Client broadcasts:

---

## 4. Server Architecture & DI Wiring
- **Interfaces & Services (`server/Services/`):**
  - `I...` (Colocated above implementation when narrow, or shared contract)
  - Thread-safety & concurrency strategy:
- **DI Registration (`server/Program.cs`):**
  - Service lifetimes (`AddSingleton`, `AddScoped`, etc.):

---

## 5. Client Architecture & Phaser 4 Presentation
- **Network Service Integration (`client/src/services/`):**
  - Hub connection methods and typed event callbacks:
- **Phaser Scenes & UI (`client/src/scenes/`, `client/src/ui/`):**
  - Visual layout, interactive grid/buttons, turn indicators, and tweens:
  - Scene `SHUTDOWN` / `DESTROY` event unsubscription plan:

---

## 6. Edge Cases, Concurrency & Lifecycle Invariants
- **Authoritative Validation:** Server handling of out-of-turn clicks, occupied cells, or invalid session states.
- **Disconnects & Abandonment:** Behavior when a player disconnects mid-match or during rematch negotiation.
- **Race Conditions:** Handling simultaneous matchmaking joins, moves, or rematch requests.

---

## 7. Implementation Plan (Phases & Steps)

### Phase 1: Server Domain, DTOs & Unit Tests
- [ ] **Step 1.1:** Implement pure domain models and xUnit tests.
  - Files: server/Domain/..., tests/Domain/...
  - Verification: `dotnet test`
- [ ] **Step 1.2:** Implement services, DTOs, SignalR Hub endpoints, and DI registration.
  - Files: server/DTOs/..., server/Services/..., server/Hubs/..., server/Program.cs
  - Verification: `dotnet build` & `dotnet test`

### Phase 2: Client Networking & Phaser UI
- [ ] **Step 2.1:** Add TypeScript DTO interfaces and `NetworkService` methods.
  - Files: client/src/types/..., client/src/services/...
  - Verification: `npm --prefix client run build`
- [ ] **Step 2.2:** Implement Phaser scenes, UI components, and shutdown cleanup.
  - Files: client/src/scenes/..., client/src/ui/...
  - Verification: `npm --prefix client run build`

### Phase 3: Polish & Pre-Commit Verification
- [ ] **Step 3.1:** Run `pre-commit-gate` (`dotnet test`, `npm --prefix client run build`, `.agents/scripts/check-architecture.ps1`).
- [ ] **Step 3.2:** Perform live 2-client browser verification.

---

## 8. Verification & Acceptance Criteria
- [ ] Server builds with zero warnings and all xUnit tests pass (`dotnet build`, `dotnet test`).
- [ ] Client compiles and bundles with zero TypeScript errors (`npm --prefix client run build`).
- [ ] Architecture linter passes (`powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1`).
- [ ] Coding standards strictly followed (C# block syntax, naming, strict TS without `any`, English invariant).
- [ ] No dangling scene listeners or unhandled disconnect states.
