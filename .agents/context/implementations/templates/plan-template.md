# Implementation Plan - [Feature or Task Name]

Date: YYYY-MM-DD

[Brief description of the problem, background context, and what the change accomplishes.]

## User Review Required

> [!IMPORTANT]
> [Critical architectural decisions, network protocol changes, or user-visible gameplay/UI behaviors requiring explicit user confirmation before proceeding.]

## Open Questions

- [Question 1 for the user regarding game rules, matchmaking, network protocol, or UI flow]
- [Question 2, or state "None."]

## Proposed Changes

### [Server / Domain / Hub Area]

#### [NEW] server/[Path/To/NewFile.cs]
- [Description of new file, interface, DTO record, or class responsibilities]

#### [MODIFY] server/[Path/To/ModifiedFile.cs]
- [Description of modifications, method signatures, or service changes]

---

### [Client / Phaser Scene / Network Service Area]

#### [NEW] client/src/[Path/To/NewFile.ts]
- [Description of new TypeScript interface, service, or Phaser scene]

#### [MODIFY] client/src/[Path/To/ModifiedFile.ts]
- [Description of modifications]

---

### [Automated Tests]

#### [NEW] tests/[Path/To/TestFile.cs]
- [Description of xUnit test scenarios covering normal, edge, and failure paths]

---

## Verification Plan

### Automated Checks
- Server compilation & xUnit tests:
```powershell
dotnet build
dotnet test
```
- Client TypeScript typecheck & production build:
```powershell
npm --prefix client run build
```
- Architecture & coding standards linter:
```powershell
powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
```

### Manual Verification
1. [Step 1 for starting the server and opening two browser client windows/tabs]
2. [Step 2 verifying specific turn sequences, win/draw states, rematch flow, or disconnect handling]
