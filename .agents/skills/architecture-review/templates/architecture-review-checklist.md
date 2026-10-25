# Architecture Review Report

**Target System / Feature:** [System / Feature Name]

**Date:** [YYYY-MM-DD]

**Verdict:** [APPROVE | REQUEST CHANGES | PENDING VERIFICATION]

---

## 1. Scope & Touched Files
- Files Audited:
  - server/...
  - client/src/...
  - tests/...

---

## 2. Validation & Compilation Gate
- `dotnet build` & `dotnet test`: [PASS / FAIL / PENDING / PROPOSED] (Actual command, exit code, errors, warnings, test count)
- `npm --prefix client run build`: [PASS / FAIL / PENDING / PROPOSED]
- `powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1`: [PASS / FAIL / PENDING / PROPOSED]
- Baseline diagnostics and unavailable checks:

---

## 3. Full-Stack Breaking Change Assessment
- [ ] **Authoritative Domain Purity:** (Domain logic in `server/Domain/` has no SignalR/HTTP coupling; server validates all moves)
- [ ] **DI & Concurrency Safety:** (Services registered in `server/Program.cs`; shared session state is thread-safe)
- [ ] **SignalR DTO & Contract Alignment:** (C# DTOs in `server/DTOs/` and TS interfaces in `client/src/types/` match)
- [ ] **Client Lifecycle & Cleanup:** (Phaser scenes delegate networking to `client/src/services/` and unsubscribe on shutdown)

---

## 4. Findings by Severity

### 🔴 Blocker (Must fix before merge/completion)
- [None / Finding list with file, line, rationale, and concrete fix]

### 🟠 Major (Architecture, concurrency, or lifecycle flaws)
- [None / Finding list with file, line, rationale, and concrete fix]

### 🟡 Minor (Standards & non-blocking structural drift)
- [None / Finding list with file, line, rationale, and concrete fix]

### ⚪ Nit (Stylistic suggestions)
- [None / Finding list with file, line, rationale, and concrete fix]

---

## 5. Summary & Next Actions
- Summary of architecture health:
- Required follow-up actions:
