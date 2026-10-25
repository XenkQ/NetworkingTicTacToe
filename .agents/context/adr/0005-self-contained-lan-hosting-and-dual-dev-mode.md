# ADR 0005: Self-Contained LAN Static Hosting and Dual Development Mode

- **Status:** Accepted
- **Date:** 2026-10-25
- **Use when:** Modifying Kestrel network binding, CORS policies, static file serving, MSBuild publish targets, or Vite development proxy configuration.

## Context

In the primary deployment scenario, one participant starts the server on their machine and a second participant on another computer in the same Local Area Network (LAN) joins simply by opening `http://<host-lan-ip>:5079` in a web browser—without installing Node.js or running a separate frontend server. Simultaneously, during day-to-day frontend work, developers need instant Vite Hot Module Replacement (HMR) without rebuilding static bundles after every TypeScript edit.

## Decision

1. **Single-Port Self-Contained LAN Hosting in ASP.NET Core:**
   - Default Kestrel binding to `http://0.0.0.0:5079` when neither `Urls` configuration nor `ASPNETCORE_URLS` is set, exposing both the SignalR hub (`/hubs/game`), `/health`, and static client assets on a single LAN-reachable port.
   - Serve built frontend assets directly from client/dist/ when running from source (`PhysicalFileProvider` registered for disposal on `ApplicationStopping` with `UseDefaultFiles`, `UseStaticFiles`, and `MapFallbackToFile("index.html")`), falling back automatically to server/wwwroot/ in standalone published builds, or returning a plain-text diagnostic fallback if client assets have not been built yet.
   - Include the `CopyClientDistToPublishWwwroot` MSBuild target in server/NetworkingTicTacToe.Server.csproj so `dotnet publish` automatically packages built client/dist/ assets into `$(PublishDir)wwwroot/`.
2. **Private-LAN Scoped CORS Policy (`LanCorsPolicy`):**
   - Permit credentialed SignalR requests (`AllowCredentials()`) strictly from loopback origins (`localhost`, `127.0.0.1`, `::1`) and private RFC1918 / link-local IPv4 LAN ranges (`10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `169.254.0.0/16`) via `IsAllowedLanOrLocalDevOrigin` in server/Program.cs, rather than opening wildcard origins.
3. **Vite Dev Server Proxy for Fast Frontend Iteration:**
   - Configure client/vite.config.ts on port `5173` to proxy `/hubs` (`ws: true`) and `/health` to `http://localhost:5079`, allowing `GameNetworkService` to use the relative hub path `'/hubs/game'` identically in both Vite HMR mode (`:5173`) and single-port Kestrel hosting (`:5079`).

## Alternatives Considered

- **Requiring Separate Frontend and Backend Servers on LAN:** Rejected. Forcing players or evaluators to run two processes and coordinate cross-port URLs degrades LAN usability and breaks the relative `'/hubs/game'` endpoint convention.
- **Unrestricted CORS (`SetIsOriginAllowed(_ => true)`):** Rejected. SignalR WebSocket negotiation with `AllowCredentials()` should not accept arbitrary public web origins when the server is bound on `0.0.0.0`.

## Consequences and Guardrails

- **Positive:**
  - Zero-configuration browser access across LAN devices from a single `dotnet run --project server` (once client/dist/ is built) or `dotnet publish` artifact, alongside full Vite HMR ergonomics during development.
  - Identical client networking code (`DEFAULT_HUB_URL = '/hubs/game'`) across development, local single-port, and LAN deployments.
- **Trade-offs & Invariants:**
  - When testing via single-port hosting (`:5079`) after modifying client TypeScript files, `npm --prefix client run build` must be executed so client/dist/ reflects the latest bundle.
  - Always dispose custom `PhysicalFileProvider` instances via `app.Lifetime.ApplicationStopping` in server/Program.cs to avoid file-watcher handle leaks.

## Authoritative Files and Related Docs

- Server hosting, CORS, & publish packaging: server/Program.cs, server/NetworkingTicTacToe.Server.csproj, server/appsettings.json, server/Properties/launchSettings.json
- Client dev proxy & relative hub URL: client/vite.config.ts, client/src/services/GameNetworkService.ts
- Related system doc: .agents/context/game-systems/signalr-network-protocol-system.md
