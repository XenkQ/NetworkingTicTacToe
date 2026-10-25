---
name: document-system
description: "Use when: creating or updating agent-facing technical documentation for a specific Networking Tic-Tac-Toe gameplay or networking system under .agents/context/game-systems/, such as Board & Rules Domain, SignalR GameHub & Protocol, Matchmaking & Session Management, or Phaser Client Scenes. Route explicit human-facing deliverables through create-user-doc."
---

# Document System Skill

Use this skill to create clear, implementation-grounded documentation for a pointed system.

## Inputs

- Target system name (example: Board & Rules System, SignalR Session System, Phaser Board UI System).
- Scope boundaries and key files (`server/`, `client/src/`, `tests/`).
- Agent-facing operational purpose. Route an explicit human-document request to create-user-doc and its index workflow; do not create .user-docs/ during ordinary system documentation work.
- Required sections or templates.

## Workflow

1. Read AGENTS.md, coding standards, relevant ADRs, and game-system documents. Consult official sources through .agents/context/technology-documentation.md for framework claims. Search agent documentation to determine whether this system is already documented; do not read .user-docs/ as operational truth.
2. If existing documentation is found:
   - Review it against the current state of the project (`server/`, `client/`, `tests/`).
   - Update the existing documentation to make it up-to-date and accurate.
3. If no existing documentation is found:
   - Identify source files and authoritative docs for the target system.
   - Extract behavior, network protocol / data flow, extension points, and invariants.
   - Write concise new documentation under .agents/context/game-systems/ focused on architecture and practical usage.
4. For both updates and new documentation, ensure you:
   - Add references to related systems and client-server ownership boundaries.
   - Include known risks, assumptions, and open questions.
   - Verify concrete claims in source and distinguish current implementation from planned behavior. Record live browser/network checks as unverified unless performed.

## Output

Produce a filled system document based on:

- .agents/skills/document-system/templates/system-document-template.md
