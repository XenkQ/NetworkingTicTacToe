# Brainstorm Brief: [Feature / Idea Name]

Date: YYYY-MM-DD

Label choices and values as confirmed, recommended, tentative, or unresolved. Existing behavior requires source evidence. Omit irrelevant sections and leave unknown parameters unresolved; template examples are not approved design values.

## 1. Context & Motivation
- Feature / Idea: [Comprehensive description of the gameplay mechanic, matchmaking flow, network feature, or UI exploration]
- Player-Facing Goal: [Direct gameplay feel, clarity, responsiveness, or multiplayer UX goal]
- Impacted Areas:
  - [Server Domain / Rules, e.g. server/Domain/]
  - [Server Services & SignalR Hub, e.g. server/Services/, server/Hubs/]
  - [Client Networking & State, e.g. client/src/services/, client/src/types/]
  - [Client Phaser Scenes & UI, e.g. client/src/scenes/, client/src/ui/]
  - [Automated Tests, e.g. tests/]

---

## 2. Explored Alternatives & Trade-Offs

### Option 1 (Selected / Recommended): [Approach Name]
- Pros:
  - [Architectural advantage, clean separation of domain and transport]
  - [Deterministic state synchronization and simple xUnit testability]
  - [Responsive Phaser UI feedback]
- Cons / Risks:
  - [Potential edge cases, disconnect/reconnect complexity, or migration overhead]

### Option 2: [Alternative Paradigm Name]
- Pros:
  - [Pros of this alternative approach]
- Cons / Risks:
  - [Why this was rejected or deprioritized, coupling risks, maintenance costs]

### Option 3 (Minimal / Build Nothing / Reuse Existing): [Baseline Option]
- Pros:
  - [Pros of minimal or reuse approach]
- Cons / Risks:
  - [Why it fails to meet gameplay or multiplayer UX goals]

---

## 3. Full-Stack & Architecture Considerations

- Authoritative Domain & Rules (`server/Domain/`):
  - [Board state representation, move validation rules, win/draw/rematch transitions]
- SignalR Protocol & DTO Contracts (`server/Hubs/`, `server/DTOs/`, `client/src/types/`):
  - [Client-to-server RPC invocations, server-to-client broadcast events, payload records]
- Session Lifecycle & Concurrency (`server/Services/`):
  - [Thread safety, player pairing, disconnect/abandonment handling]
- Phaser 4 Scenes, Input & Visuals (`client/src/scenes/`, `client/src/ui/`):
  - [Canvas layout, animations/tweens, turn indicators, scene shutdown listener cleanup]

---

## 4. Key Decisions & Detailed Specifications

### A. Gameplay / State Flow
1. **[Flow / State Transition 1]**:
   - [Trigger condition, server validation, broadcast payload, client visual reaction]
2. **[Flow / State Transition 2]**:
   - [Error/rejection handling, edge cases, disconnect recovery]

### B. Design Parameters and Decision State
- [Parameter 1, e.g. Board Size, Turn Timer, Reconnect Grace Period]
- [Unresolved choices and values; distinguish recommendation from user confirmation]

---

## 5. Next Step & Implementation Scope

- Recommended Next Skill: [e.g. gameplay-spec-writing, architecture-review]
- Target Implementation Scope:
  - server/Domain/[File].cs
  - server/Hubs/[File].cs
  - client/src/scenes/[Scene].ts
  - client/src/services/[Service].ts
  - tests/[TestFile].cs
