---
name: create-user-doc
description: "Use when: creating or updating human-facing, user-oriented project documentation in .user-docs/ upon explicit user request (e.g., explaining gameplay concepts, systems, network protocols, architecture, or workflows in plain, intuitive language for human readers)."
---

# Create User Doc Skill

Use this skill when the user explicitly requests documentation intended for humans (developers, designers, testers, or players) in the `.user-docs/` folder.

## Core Rules

1. **Explicit Request Only**: Do not create or update files in `.user-docs/` during normal feature development, bug fixes, or refactoring tasks unless the user explicitly requests a user-facing document.
2. **Grounding in Code**: Base all explanations on actual code (`server/`, `client/`, `tests/`) and `.agents/context/` to ensure technical accuracy.
3. **Human-Friendly Style**: Write in clear, accessible language. Focus on concepts, mental models, workflows, visual diagrams (Mermaid), and practical configuration rather than raw code dumps.
4. **Target Location**: Place all generated documents in `.user-docs/<topic-name>.md` (kebab-case).
5. **Update Index**: Always update `.user-docs/README.md` to list the newly created or updated document.
6. **Isolation Guarantee**: Agents must not use `.user-docs/` as an operational source of truth for implementation logic. Technical decisions must rely on source code and `.agents/context/`.

## Inputs

- **Target Topic / System**: (e.g., SignalR Matchmaking Flow, Board & Win Evaluation Rules, Client Scene Architecture, Local LAN Setup Guide).
- **Target Audience**: (e.g., New Developer onboarding, Tester/Player guide).
- **Scope & Focus**: Specific aspects the user wants explained.

## Workflow

1. **Research the Codebase**:
   - Inspect authoritative runtime files in `server/`, `client/`, and `tests/`.
   - Review relevant `.agents/context/game-systems/` documentation for architectural context.

2. **Structure the Human Document**:
   - Follow the structure defined in `.agents/skills/create-user-doc/templates/user-doc-template.md`.
   - Include:
     - **Overview / What is this?**: Clear high-level explanation.
     - **Core Concepts**: Glossary and mental model breakdown.
     - **How It Works (Visual Flow)**: Mermaid diagram showing how network messages or game states flow.
     - **Developer / User Guide**: Step-by-step instructions.
     - **Key Configuration & Parameters**: Tables explaining key constants, endpoints, or options.
     - **FAQ / Troubleshooting**: Common questions and pitfalls.

3. **Write Document**:
   - Save to `.user-docs/<topic-name>.md`.

4. **Update Directory Index**:
   - Add entry to the table/list in `.user-docs/README.md`.

## Output

- A clean, well-formatted Markdown document in `.user-docs/<topic-name>.md`.
- Updated `.user-docs/README.md`.
