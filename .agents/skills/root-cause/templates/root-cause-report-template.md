# Root Cause Report: [Bug / Issue Summary]

**Date:** [YYYY-MM-DD]

**Status:** [Confirmed cause | Working hypothesis | Insufficient evidence] (Read-Only)

**Defect Category:** [Domain Logic | SignalR Protocol/DTO | Session Concurrency/Disconnect | Server DI/Config | Phaser Scene Lifecycle/UI]

---

## 1. Symptom & Reproduction
- **Observed Behavior:** What happens vs. what was expected.
- **Error / Stack Trace / Failing Test:** (If available)
- **Reproduction Conditions:** When and how the defect triggers (e.g., specific board state, simultaneous clicks, tab close).

---

## 2. Root Cause Analysis
- **Defect Location:** server/... or client/src/... (Class/file, method, line range)
- **Mechanism of Failure:** Explain the causal path with evidence (e.g., DI lifetime mismatch, missing scene listener cleanup, race condition in session store).
- **Layer Boundaries Involved:** How the defect crosses client, SignalR hub, session service, or domain model.

---

## 3. Minimal Proposed Fix
- **Proposed Approach:** The smallest, safest change that fixes the defect without side effects.
- **Target Files to Modify:**
  - server/...
  - client/src/...
  - tests/...
- **Concrete Code Snippet / Fix Concept:**
  ```csharp
  // Suggested fix snippet (C# or TypeScript)
  ```

---

## 4. Regression Risks & Verification Plan

The steps below are proposed post-fix checks. They do not certify an unimplemented fix.
- **Potential Side Effects:** Any related systems or network flows that could be affected by this fix.
- **Verification Steps:**
  1. Server Build & xUnit Tests: `dotnet build` and `dotnet test`
  2. Client Build: `npm --prefix client run build`
  3. Live Browser Verification: Step-by-step instructions to verify the fix across two connected client tabs.

## 5. Investigation Checks Actually Performed

- Inspected evidence / executed command and result:
- Reproduction performed: [Yes with evidence | No]
- Baseline failures, unavailable checks, and remaining hypotheses:
