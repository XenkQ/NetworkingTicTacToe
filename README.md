# Networking Tic-Tac-Toe

A 2-player networked turn-based Tic-Tac-Toe web game built with an authoritative server architecture.

## Technology Stack

- **Client**: Phaser 4, TypeScript, Vite, `@microsoft/signalr`
- **Server**: C#, .NET 10, ASP.NET Core, SignalR
- **Testing**: xUnit (.NET 10)

## Repository Layout

- `client/` — Phaser 4 + TypeScript frontend SPA
- `server/` — ASP.NET Core 10 + SignalR authoritative game server
- `tests/` — xUnit test suite for domain logic and session rules
- `.agents/` — AI agent operational context, coding standards, scripts, and workflows
- `AGENTS.md` — Vendor-neutral agent entrypoint and workflow guide

## Development & Validation

- **Server Build & Tests**:
  ```powershell
  dotnet build
  dotnet test
  ```
- **Client Build**:
  ```powershell
  npm --prefix client run build
  ```
- **Architecture Linter**:
  ```powershell
  powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
  ```
