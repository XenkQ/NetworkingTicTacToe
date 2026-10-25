# Server & Tests Coding Standards (C# / .NET 10 / ASP.NET Core / xUnit)

## Purpose

This document defines the C# coding standards, naming conventions, member ordering, dependency injection rules, and xUnit testing practices for the server (server/) and automated test suite (tests/).

Use this document alongside:
- .agents/context/shared-coding-standards.md for cross-stack architectural rules, English Language Invariant, and DTO contract alignment.
- .agents/context/project-coding-standards.md for the top-level standards index.

## Scope

Apply these rules to all C# files in:
- server/ (Program.cs, Domain/, Services/, Hubs/, DTOs/)
- tests/ (xUnit unit and integration test suites)

---

## 1) C# Language & Framework Idioms

- **File-Scoped Namespaces**: Use file-scoped namespaces in all C# files (`namespace NetworkingTicTacToe.Server.Domain;`).
- **Nullable Reference Types**: Enable and respect nullable reference types (`<Nullable>enable</Nullable>`). Avoid null-forgiving operators (`!`) in production code unless structurally guaranteed and justified.
- **Immutable DTO Records & Modular Files**: Use immutable `record` types for network DTOs and value payloads (`public sealed record MakeMoveRequestDto(int Row, int Col);`). Organize server/DTOs/ into modular files: group tightly coupled request/response pairs or parent-child snapshot records into descriptive plural files (e.g., server/DTOs/MoveDtos.cs, server/DTOs/GameStateDtos.cs) and place standalone DTOs in individual files named after the type (e.g., server/DTOs/JoinGameResponseDto.cs). Never combine unrelated DTOs into a single monolithic file.
- **LINQ Usage**: Standard `System.Linq` is **permitted and encouraged** on the server and in xUnit tests for readable collection queries, board state evaluation, and assertions.
- **One Primary Type Per File**: Keep each file focused on one primary class, service, hub, standalone DTO, or test fixture. Supporting small enums, nested DTO records, or narrow interfaces may live in the same file only when tightly related to that primary type.
- **Interface Colocation**: Define a narrow interface and its primary implementation in the same file (with the interface placed above the class) whenever the interface is owned and consumed primarily for dependency injection of that single implementation. Shared contracts consumed across multiple unrelated modules or layers may live in dedicated contract/DTO files.

---

## 2) C# Naming Conventions

- **Types, Classes, Records, Structs, Enums, Namespaces**: `PascalCase`.
- **Interfaces**: Prefix with capital `I` (`IGameSessionService`, `IWinEvaluator`).
- **Public Methods, Properties, Events**: `PascalCase`. Events use `OnX` naming when declared.
- **Private Fields**: `_camelCase` (e.g., `private readonly IGameSessionService _sessionService;`). Prefer `readonly` whenever assigned only in the constructor.
- **Boolean Fields & Variables**: Prefer `_isX`, `_hasX`, `_canX` for private fields (or `isX`, `hasX`, `canX` for local variables and parameters).
- **Constants**: `UPPER_SNAKE_CASE` (e.g., `public const int BOARD_SIZE = 3;`). Place shared domain constants in a `Constants/` folder or static `*Constants` class under the owning domain when shared across classes.
- **Async Methods**: Suffix asynchronous methods returning `Task` or `Task<T>` with `Async` (except SignalR Hub RPC methods where client invocation names follow the hub contract, e.g., `MakeMove` or `JoinGame`).

---

## 3) C# Member Ordering & Method Syntax

Order class members consistently from top to bottom:

1. Constants (`const`, `static readonly`).
2. Private `readonly` injected dependencies and state fields (`_camelCase`).
3. Constructors.
4. Public properties.
5. Public methods.
6. Private helper methods.

### Method Block Syntax Requirement
- Use standard curly-brace block bodies `{}` with explicit `return` statements for all methods and functions.
- Expression-bodied syntax (`=>`) is permitted for read-only properties, indexers, and simple switch expressions, but **never** for methods (enforced by rule `CS-ARCH003` in .agents/scripts/check-architecture.ps1).

---

## 4) Dependency Injection & State Safety

- **Explicit Registration**: Register server services explicitly in server/Program.cs (or dedicated service registration extensions) using ASP.NET Core `IServiceCollection` (`AddSingleton`, `AddScoped`, `AddTransient`).
- **Constructor Injection**: Use constructor injection for all runtime services and SignalR Hubs.
- **No Static Mutable State**: Do not use static mutable singletons (`public static Instance`) to share runtime state (enforced by `CS-ARCH001`). Use thread-safe singleton services registered in the DI container (e.g., `ConcurrentDictionary` or explicit locking for active sessions).
- **No Public Mutable Fields**: Encapsulate state behind properties (`{ get; init; }` or `{ get; private set; }`) or private fields rather than public mutable fields (`CS-ARCH002`).
- **Fail-Fast Validation**:
  - Guard constructor dependencies (`ArgumentNullException.ThrowIfNull(...)` where appropriate).
  - In domain logic, validate move coordinates, turn ownership, and session state explicitly and return deterministic result objects (or throw domain exceptions caught cleanly at the boundary) rather than silently ignoring invalid state.

---

## 5) Domain Isolation & SignalR Hubs

- **Pure Domain Layer**: Classes in server/Domain/ must remain 100% pure C# and free of `Microsoft.AspNetCore.SignalR` or HTTP transport dependencies (`CS-ARCH005`) so they can be tested deterministically in isolation.
- **Thin SignalR Hubs**: Keep SignalR hubs in server/Hubs/ focused on connection lifecycle (`OnConnectedAsync`, `OnDisconnectedAsync`), group management, and delegating commands to services in server/Services/.
- **Strongly Typed Hubs**: Prefer `Hub<IGameClient>` over untyped string-based `Clients.All.SendAsync(...)` calls so outbound server-to-client events are compile-time checked.

---

## 6) xUnit Testing Conventions (tests/)

- **Test Naming**: Name test methods clearly using `MethodName_Scenario_ExpectedResult` or descriptive specification naming.
- **Test Structure**: Structure every test using Arrange-Act-Assert (`// Arrange`, `// Act`, `// Assert`).
- **Fact vs. Theory**: Use `[Fact]` for single deterministic scenarios and `[Theory]` with `[InlineData]` for parameterized board, win-line, coordinate boundary, and move-validation checks.
- **Fast & Deterministic**: Keep unit tests for server/Domain/ fast, deterministic, and independent of live network sockets or external timers.

---

## 7) Server Agent Execution Checklist

Before finalizing any C# server or test change:

1. `dotnet build` succeeds with 0 errors and 0 warnings.
2. `dotnet test` passes all xUnit tests cleanly.
3. .agents/scripts/check-architecture.ps1 passes with 0 C# violations (`CS-ARCH001` through `CS-ARCH005`).
4. All methods use `{}` block syntax; constants use `UPPER_SNAKE_CASE`; interfaces use `I` prefix; private fields use `_camelCase`.
5. Narrow single-implementation interfaces are colocated above their implementation class.
6. Domain classes in server/Domain/ have zero SignalR or HTTP dependencies.
7. New domain rules and edge cases are covered by deterministic xUnit `[Fact]` or `[Theory]` tests.
