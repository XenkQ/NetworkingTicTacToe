---
name: game-brainstorm
description: "Use when: exploring gameplay ideas, new mechanics, rule variations, matchmaking modes, network features, UI/UX flows, or architecture trade-offs before any code, spec, or asset is created. Triggers: brainstorm, game design idea, should we build this, let's think this through, game mode idea, mechanic concept."
---

# Game Brainstorm Skill

Use this skill for divergent gameplay, networking, UX, and architecture exploration before creating any technical spec or code change. It guides the conversation to question assumptions, evaluate player feel and network determinism, compare architectural options, and converge on a concrete handoff brief.

## Hard Gate (Read-Only)

During the brainstorm phase, do not edit runtime repository code in `server/`, `client/`, or `tests/`, or begin implementation.
The only persistent artifact this skill produces is the final, detail-rich brainstorm summary saved to .agents/context/brainstorming-summaries/[feature-name]-brainstorm-summary.md when the user confirms the direction or concludes the session.

## Required Sources

Ground the exploration in the project context:

- AGENTS.md
- .agents/context/ai-game-dev-best-practices.md
- .agents/context/project-coding-standards.md
- Relevant game system docs under .agents/context/game-systems/
- Existing brainstorm summaries under .agents/context/brainstorming-summaries/

## Workflow

1. Frame the Topic
   - Restate the idea, question, or pain point clearly.
   - Classify the request: game rule/board mechanic, matchmaking/room feature, SignalR protocol extension, Phaser UI/animation polish, or architecture simplification.
   - Read just enough of the codebase to speak concretely about existing systems and constraints (`server/Domain/`, `server/Hubs/`, `client/src/scenes/`, `client/src/services/`).

2. Explore Alternatives (Diverge)
   - Ask clarifying questions one at a time regarding motivation, player UX, edge cases (disconnects, race conditions), and constraints.
   - Always present at least two viable implementation alternatives plus the baseline option ("build nothing / keep simple / reuse existing flow").
   - Compare alternatives across key full-stack dimensions:
     - Player Feel & UX: visual clarity, turn feedback, responsiveness in Phaser 4.
     - Authoritative Server & Protocol: domain purity, SignalR payload simplicity, state synchronization.
     - Concurrency & Reliability: disconnect handling, reconnection, race-condition safety.
     - Testability: ease of deterministic verification in xUnit.

3. Reality-Check Existing Systems
   - Verify if the feature overlaps with existing capabilities in `server/Domain/`, `server/Services/`, or `client/src/`.
   - Check if an existing interface, service, or DTO can be extended cleanly instead of building a parallel flow.

4. Converge & Route (Decide)
   - Summarize the trade-offs and recommend the cleanest, most reversible path.
   - Clarify any non-obvious game rule or network flow assumptions with the user.
   - Identify the appropriate next skill to execute the work:
     - `gameplay-spec-writing`: for features requiring multi-step client-server specification and implementation planning.
     - `architecture-review`: for evaluating structural impacts on existing services and scenes.

5. Produce & Save the Brainstorm Summary
   - When the brainstorm session concludes or is confirmed by the user, produce a comprehensive, detail-rich brief based on .agents/skills/game-brainstorm/templates/brainstorm-brief-template.md.
   - Save the completed summary directly in the repository at .agents/context/brainstorming-summaries/[feature-name]-brainstorm-summary.md (use kebab-case without date prefix in the filename; specify the date inside the document body).
   - Include technical and gameplay details supported by the discussion and inspected source. Label every decision/value as confirmed, recommended, tentative, or unresolved. Do not invent settled parameters, state transitions, or implementation choices to fill the template. Distinguish existing behavior from a design possibility, and omit irrelevant sections.

## Output

Produce and save a completed summary in .agents/context/brainstorming-summaries/ following the template with:

1. Context & Motivation: Detailed feature description, player-facing goals, and breakdown of impacted server/client areas.
2. Explored Alternatives & Trade-offs: Detailed comparison of options with pros, cons, and architectural risks.
3. Full-Stack & Architecture Considerations: Domain state rules, SignalR protocol contracts, session lifecycle & disconnect handling, and Phaser UI/scene presentation.
4. Key Decisions & Detailed Specifications: State transitions, edge cases, and decision state.
5. Next Steps & Target Scope: Recommended next skill and explicit list of target server, client, and test files.
