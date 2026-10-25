# Networking Tic-Tac-Toe Coding Standards Index

## Purpose

This document serves as the central navigation router and quick-reference index for the project's modular coding standards across the C# (.NET 10) server/test projects and the TypeScript (Phaser 4) client application.

---

## 1) Modular Standards Routing Guide

Read the targeted coding standards module that matches the scope of your task:

- **Shared Architectural & Repository Rules**:
  - Path: .agents/context/shared-coding-standards.md
  - Read when: working across any repository file, designing client-server DTO contracts, enforcing the English Language Invariant, structuring files/interfaces, or writing agent documentation.
- **Server & Automated Tests Standards (C# / .NET 10 / ASP.NET Core / SignalR / xUnit)**:
  - Path: .agents/context/server-coding-standards.md
  - Read when: writing, refactoring, or reviewing C# code under server/ or tests/.
- **Client Application Standards (TypeScript / Phaser 4 / Vite / `@microsoft/signalr`)**:
  - Path: .agents/context/client-coding-standards.md
  - Read when: writing, refactoring, or reviewing TypeScript/Phaser code under client/.

---

## 2) Scope & Migration Policy

Apply these rules to:

1. New files (server/, tests/, client/, .agents/).
2. New code added to existing files.
3. Modified sections in refactors.

When a file contains legacy style, prefer incremental migration in touched areas instead of broad formatting-only rewrites.

---

## 3) Top-Level Summary for Quick Scanning

### Shared Rules (.agents/context/shared-coding-standards.md)
- **English Language Invariant**: All code identifiers, comments, XML/JSDoc docstrings, logs, templates, and documentation must be strictly in English.
- **Authoritative Server**: The server owns canonical board state, move legality, turn order, and win/draw evaluation; the client dispatches intent and renders state.
- **Contract Alignment**: Synchronize C# `PascalCase` positional records in server/DTOs/ with TypeScript `camelCase` DTO interfaces in client/src/types/ (or client/src/models/).
- **One Primary Type Per File**: Keep one primary runtime class, service, hub, or scene per file; colocate narrow single-implementation C# interfaces above their implementation.
- **Zero Warnings Policy**: Resolve all compiler (`dotnet build`, `tsc`) and architecture linter (.agents/scripts/check-architecture.ps1) warnings before finishing a task.

### Server & Tests Rules (.agents/context/server-coding-standards.md)
- **Idioms**: File-scoped namespaces, nullable reference types enabled, immutable DTO `record` types, and `System.Linq` permitted and encouraged.
- **Naming**: `PascalCase` for types/methods/properties/events (`OnX`), `I` prefix for interfaces, `_camelCase` for private fields (`_isX`, `_hasX`, `_canX` for booleans), `UPPER_SNAKE_CASE` for constants, and `*Async` suffix for async methods (except Hub RPC contract names).
- **Member Ordering**: Constants -> Private `readonly` fields -> Constructors -> Public properties -> Public methods -> Private helpers.
- **Method Block Syntax**: Use `{}` block bodies with explicit `return` statements for all methods; never use `=>` for methods (`CS-ARCH003`).
- **DI & Domain Purity**: Constructor injection via ASP.NET Core `IServiceCollection`; no static mutable singletons (`CS-ARCH001`); no public mutable fields (`CS-ARCH002`); server/Domain/ must remain pure and free of SignalR/HTTP dependencies (`CS-ARCH005`).
- **xUnit Tests**: Arrange-Act-Assert structure, `MethodName_Scenario_ExpectedResult` naming, `[Fact]` and `[Theory]` coverage for deterministic domain logic.

### Client Rules (.agents/context/client-coding-standards.md)
- **Strict Type Safety**: Maintain `"strict": true` in client/tsconfig.json; zero explicit `any` (`TS-ARCH001`); no `@ts-ignore` or `@ts-nocheck` (`TS-ARCH002`).
- **Naming**: `PascalCase` for classes, types, enums, and DTO interfaces (no `I` prefix on data DTOs); `camelCase` for variables, functions, methods, and properties; `_camelCase` for internal mutable fields; `UPPER_SNAKE_CASE` for constants.
- **Phaser & SignalR Separation**: Phaser Scenes in client/src/scenes/ own visual presentation and input; services in client/src/services/ own `HubConnection`. Never instantiate `HubConnectionBuilder` in a Scene (`TS-ARCH003`).
- **Scene Lifecycle**: Reset per-match state in `init()` / `create()` and always unsubscribe network/input listeners and clean up tweens/timers in `Phaser.Scenes.Events.SHUTDOWN`.

---

## 4) Verification Commands

1. Server build & xUnit tests: `dotnet build` and `dotnet test`
2. Client TypeScript & Vite build: `npm --prefix client run build`
3. Architecture & standards audit: `powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1`
