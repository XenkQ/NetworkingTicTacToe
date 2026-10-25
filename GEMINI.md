# Gemini & Antigravity Agent Directives

## Purpose

This document defines high-priority runtime directives specifically for Gemini and Antigravity agents operating in this repository.

All project architecture, coding standards, invariants, and operational workflows are defined canonically in AGENTS.md as the single vendor-neutral source of truth.

## Antigravity UI Artifact Handling

1. **In-Repository Precedence**:
   - When Antigravity requests creating an artifact (such as implementation plans, walkthroughs, or preview documents in the brain artifact directory), the agent MUST ALWAYS write the primary authoritative markdown document directly to the repository under `.agents/context/implementations/plans/` (or `summaries/`) first.
   - The in-repo markdown file is the single source of truth for version control.
   - NEVER save implementation plans, brainstorm summaries, or task summaries exclusively to IDE-specific external artifact directories.

2. **Core Invariants Enforcement**:
   - Strictly enforce the **Implementation Lifecycle**, **Multi-Agent & Temporary Files Invariant** (all scratch and handoff data strictly in `.agents/context/tmp/`), and **English Language Invariant** as specified in AGENTS.md.
