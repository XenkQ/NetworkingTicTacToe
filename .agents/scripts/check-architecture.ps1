<#
.SYNOPSIS
    Architecture & Coding Standard Linter for Networking Tic-Tac-Toe (C# Server/Tests & TypeScript Client).
.DESCRIPTION
    Scans server/, tests/, and client/src/ for architectural anti-patterns, singleton leaks,
    public mutable fields, expression-bodied methods in C#, explicit 'any' in TypeScript,
    and direct HubConnection instantiation inside Phaser scenes.
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .agents/scripts/check-architecture.ps1
#>

param(
    [string[]]$CSharpPaths = @("server", "tests"),
    [string[]]$TypeScriptPaths = @("client/src")
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Running Networking Tic-Tac-Toe Architecture Linter" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$csFiles = @()
foreach ($path in $CSharpPaths) {
    if (Test-Path $path) {
        $csFiles += Get-ChildItem -Path $path -Recurse -Filter "*.cs" | Where-Object {
            $_.FullName -notmatch "[\\/](bin|obj)[\\/]"
        }
    }
}

$tsFiles = @()
foreach ($path in $TypeScriptPaths) {
    if (Test-Path $path) {
        $tsFiles += Get-ChildItem -Path $path -Recurse -Filter "*.ts" | Where-Object {
            $_.FullName -notmatch "[\\/](node_modules|dist)[\\/]"
        }
    }
}

$totalFiles = $csFiles.Count + $tsFiles.Count
$violationCount = 0
$warningCount = 0

Write-Host "Found $($csFiles.Count) C# file(s) and $($tsFiles.Count) TypeScript file(s) to analyze.`n" -ForegroundColor Gray

# C# Rules
$csRules = @(
    @{
        Id = "CS-ARCH001"
        Name = "Static Singleton Anti-Pattern"
        Severity = "Error"
        Pattern = "\bpublic\s+static\s+[A-Za-z0-9_<>.]+\s+(Instance|instance)\b"
        Description = "Static singleton instance leaks state. Register services in ASP.NET Core DI (IServiceCollection) instead."
    },
    @{
        Id = "CS-ARCH002"
        Name = "Public Mutable Field"
        Severity = "Warning"
        Pattern = "^\s*public\s+(?!const\b|static\b|class\b|struct\b|record\b|enum\b|interface\b|event\b|override\b|virtual\b|abstract\b|sealed\b|async\b|readonly\b)[A-Za-z0-9_<>\[\]?,\s]+\s+[a-zA-Z0-9_]+\s*;"
        Description = "Prefer properties ({ get; init; } or { get; private set; }) or private readonly fields over public mutable fields."
    },
    @{
        Id = "CS-ARCH003"
        Name = "Expression-Bodied Method Syntax Violation"
        Severity = "Warning"
        Pattern = "^\s*(public|private|protected|internal)\s+(static\s+|virtual\s+|override\s+|async\s+)*[A-Za-z0-9_<>\[\]?,\s]+\s+[A-Za-z0-9_]+\s*\([^)]*\)\s*=>"
        Description = "Do not use expression-bodied arrow syntax (=>) for methods. Use standard block bodies {} with curly braces."
    },
    @{
        Id = "CS-ARCH004"
        Name = "Interface Naming Convention Violation"
        Severity = "Warning"
        Pattern = "^\s*(public|internal|private)?\s*interface\s+(?!I[A-Z])[A-Za-z0-9_]+"
        Description = "C# interface names must start with a capital 'I' (e.g., IGameSessionService)."
    },
    @{
        Id = "CS-ARCH005"
        Name = "SignalR Transport Dependency in Domain Layer"
        Severity = "Error"
        Pattern = "\busing\s+Microsoft\.AspNetCore\.SignalR\b"
        RestrictedToPathKeyword = "Domain"
        Description = "Domain classes under server/Domain/ must remain pure and free of SignalR transport dependencies."
    }
)

# TypeScript Rules
$tsRules = @(
    @{
        Id = "TS-ARCH001"
        Name = "Banned Explicit 'any' Type"
        Severity = "Error"
        Pattern = "(:\s*any\b|<\s*any\s*>|\bas\s+any\b)"
        Description = "Explicit 'any' is banned under strict TypeScript standards. Use explicit interfaces, types, or 'unknown' with type guards."
    },
    @{
        Id = "TS-ARCH002"
        Name = "Banned TypeScript Error Suppression"
        Severity = "Error"
        Pattern = "@ts-ignore|@ts-nocheck"
        Description = "Do not suppress TypeScript compiler errors with @ts-ignore or @ts-nocheck."
    },
    @{
        Id = "TS-ARCH003"
        Name = "Direct SignalR HubConnection in Phaser Scene"
        Severity = "Error"
        Pattern = "\bHubConnectionBuilder\b"
        RestrictedToPathKeyword = "scenes"
        Description = "Phaser scenes must not build SignalR connections directly. Delegate networking to client/src/services/."
    }
)

function Invoke-FileAudit {
    param(
        [System.IO.FileInfo[]]$Files,
        [hashtable[]]$Rules
    )

    foreach ($file in $Files) {
        $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "").Replace("\", "/")
        $lines = Get-Content -Path $file.FullName
        $inBlockComment = $false

        for ($i = 0; $i -lt $lines.Count; $i++) {
            $lineNum = $i + 1
            $lineText = $lines[$i]
            $trimmedLine = $lineText.Trim()

            if ($trimmedLine.StartsWith("/*")) {
                $inBlockComment = $true
            }
            if ($inBlockComment) {
                if ($trimmedLine.EndsWith("*/") -or $trimmedLine.Contains("*/")) {
                    $inBlockComment = $false
                }
                continue
            }

            if ($trimmedLine.StartsWith("//")) {
                continue
            }

            foreach ($rule in $Rules) {
                if ($rule.RestrictedToPathKeyword -and ($relativePath -notlike "*$($rule.RestrictedToPathKeyword)*")) {
                    continue
                }

                if ($lineText -match $rule.Pattern) {
                    $fileLink = "file:///$($file.FullName.Replace('\', '/'))#L$lineNum"

                    if ($rule.Severity -eq "Error") {
                        $script:violationCount++
                        Write-Host "[$($rule.Id)] ERROR: $($rule.Name)" -ForegroundColor Red
                    } else {
                        $script:warningCount++
                        Write-Host "[$($rule.Id)] WARN: $($rule.Name)" -ForegroundColor Yellow
                    }

                    Write-Host "  File: $fileLink" -ForegroundColor White
                    Write-Host "  Line ${lineNum}: $trimmedLine" -ForegroundColor Gray
                    Write-Host "  Note: $($rule.Description)`n" -ForegroundColor DarkGray
                }
            }
        }
    }
}

Invoke-FileAudit -Files $csFiles -Rules $csRules
Invoke-FileAudit -Files $tsFiles -Rules $tsRules

$errorColor = if ($violationCount -gt 0) { "Red" } else { "Green" }
$warningColor = if ($warningCount -gt 0) { "Yellow" } else { "Green" }

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Architecture & Standards Audit Summary" -ForegroundColor Cyan
Write-Host " Total Files Audited: $totalFiles ($($csFiles.Count) C#, $($tsFiles.Count) TS)" -ForegroundColor White
Write-Host " Errors Found:        $violationCount" -ForegroundColor $errorColor
Write-Host " Warnings Found:      $warningCount" -ForegroundColor $warningColor
Write-Host "==========================================================" -ForegroundColor Cyan

if ($violationCount -gt 0) {
    Write-Host "`nResult: Audit FAILED with $violationCount critical error(s)." -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nResult: Audit PASSED cleanly!" -ForegroundColor Green
    exit 0
}
