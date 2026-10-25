# Technology Documentation for Networking Tic-Tac-Toe

## Purpose

This file provides official documentation links and reference guidance for the core technologies used in this repository.

## Documentation-First Rule

Agents must consult official documentation listed here before relying on general model memory when framework or library behavior is in question.

Execution policy:

1. Read the relevant official documentation first.
2. Apply project-specific constraints from AGENTS.md and .agents/context/project-coding-standards.md (along with .agents/context/shared-coding-standards.md, .agents/context/server-coding-standards.md, and .agents/context/client-coding-standards.md).
3. If documentation is ambiguous, ask the user instead of guessing.
4. If implementation behavior in project code intentionally differs from default docs, preserve project behavior and request clarification if needed.

## Official Documentation Links

### Phaser 4 (Client Game Framework)

- Official docs: https://docs.phaser.io/phaser/getting-started/what-is-phaser
- API Reference: https://newdocs.phaser.io/
- GitHub repository: https://github.com/phaserjs/phaser
- Use for: Game configuration, Scene lifecycle (`init`, `preload`, `create`, `update`, `shutdown`), GameObjects, Input handling, Tweens, and Canvas/WebGL rendering.

### TypeScript & Vite (Client Tooling)

- TypeScript Handbook: https://www.typescriptlang.org/docs/
- Vite Documentation: https://vite.dev/guide/
- Use for: Strict type checking (`tsconfig.json`), module resolution, dev server configuration, and WebSocket proxy setup (`server.proxy` in `vite.config.ts`).

### ASP.NET Core & SignalR (.NET 10 Server)

- ASP.NET Core Overview: https://learn.microsoft.com/en-us/aspnet/core/
- ASP.NET Core SignalR Documentation: https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction
- Strongly Typed Hubs: https://learn.microsoft.com/en-us/aspnet/core/signalr/hubs#strongly-typed-hubs
- Dependency Injection in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection
- Use for: `Hub<T>` lifecycle (`OnConnectedAsync`, `OnDisconnectedAsync`), SignalR groups, JSON serialization options, DI service lifetimes (`Singleton`, `Scoped`, `Transient`), and CORS / static file hosting.

### SignalR JavaScript / TypeScript Client (`@microsoft/signalr`)

- Official docs: https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client
- Use for: `HubConnectionBuilder`, `withAutomaticReconnect`, event registration (`connection.on` / `connection.off`), RPC invocation (`connection.invoke`), and connection state transitions.

### xUnit (.NET Automated Testing)

- Official docs: https://xunit.net/#documentation
- Getting started with .NET CLI: https://xunit.net/docs/getting-started/v2/cmdline
- Use for: `[Fact]`, `[Theory]`, `[InlineData]`, assertions, and test fixture lifecycle.

## Usage Guidance for Agents

1. Do not invent framework APIs or configuration options when official documentation is available.
2. Prefer strongly typed SignalR hubs (`Hub<IGameClient>`) on the server and typed wrapper methods on the client over untyped magic strings scattered across scenes.
3. Ensure JSON casing rules (default camelCase in ASP.NET Core SignalR JSON protocol) match between C# `record` properties (`PascalCase` in C#) and TypeScript interface properties (`camelCase` in JSON/TS).
4. For version-specific API uncertainty (such as Phaser 4 RC/beta API nuances or .NET 10 features), verify installed package versions in `client/package.json` and `server/*.csproj` first.

## Cross-References

- See AGENTS.md for architecture and repository-specific working rules.
- See .agents/context/ai-game-dev-best-practices.md for networked gameplay, validation, and review practices.
- See .agents/context/project-coding-standards.md for the coding standards index routing to .agents/context/shared-coding-standards.md, .agents/context/server-coding-standards.md, and .agents/context/client-coding-standards.md.
