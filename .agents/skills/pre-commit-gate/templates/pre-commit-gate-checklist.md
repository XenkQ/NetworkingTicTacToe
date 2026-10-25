# Pre-Commit Gate Checklist

Date: [YYYY-MM-DD]
Changed scope: [Files / domains]
Mode: [Verification only | Authorized scoped repairs]
Verdict: [PASS | FAIL | PENDING]

## Scope

- Files inspected and authorized untracked additions:
- Pre-existing user changes:
- Repair authority, if applicable:

## Gate Results

| Gate | Status | Evidence / reason |
| --- | --- | --- |
| 1. Server build & xUnit tests | [PASS / FAIL / PENDING / N/A] | [`dotnet build` & `dotnet test` exit codes, warnings, passed tests] |
| 2. Client TypeScript & Vite build | [PASS / FAIL / PENDING / N/A] | [`npm --prefix client run build` exit code and output] |
| 3. DTO, SignalR & DI contracts | [PASS / FAIL / PENDING / N/A] | [Server-client DTO parity, Hub RPC signatures, `Program.cs` DI bindings] |
| 4. Standards & architecture linter | [PASS / FAIL / PENDING / N/A] | [`check-architecture.ps1` result, C#/TS standards, scene cleanup] |
| 5. Git & workspace hygiene | [PASS / FAIL / PENDING / N/A] | [No stray temp files outside `.agents/context/tmp/`, user changes preserved] |
| 6. Lifecycle artifacts | [PASS / FAIL / PENDING / N/A] | [Durable plan and summary paths under `.agents/context/implementations/`] |

N/A requires a scope-based reason. Unavailable checks are PENDING. Overall PASS requires every applicable gate to pass.

## Findings and Repairs

- Findings reported without edits:
- Authorized corrections actually applied:
- Baseline versus introduced diagnostics:
- Remaining blockers and missing authority:

## Next Actions

- [ ] All applicable gates passed with evidence.
- [ ] Required manual 2-client browser checks completed, or explicitly pending.
- Commit/merge authority and action taken: [Not requested / Authorized / Executed with reference]
