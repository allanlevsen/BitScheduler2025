# .NET 10 Upgrade Plan

## Overview

**Target**: Upgrade all projects in the solution from .NET 9 to .NET 10
**Scope**: 8 SDK-style projects, 22 NuGet packages, and a straightforward all-at-once upgrade with limited code fixes expected

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 8 projects, all on .NET 9, SDK-style throughout, low assessed project difficulty, clear package upgrade paths, and no major structural migration required.

## Tasks

### 01-update-target-frameworks: Update target frameworks across all projects

Move every project in the solution from `net9.0` to `net10.0` so the solution is consistently aligned on the new framework baseline. This includes application, library, and test projects.

**Done when**: Every project file in the solution targets .NET 10 and solution restore still succeeds.

---

### 02-upgrade-package-dependencies: Upgrade package references for .NET 10 compatibility

Update the solution's package references to the recommended .NET 10-compatible versions, including Aspire, ASP.NET Core, Entity Framework Core, Microsoft.Extensions, and OpenTelemetry packages. Address the reported security vulnerability and deprecated package findings as part of this work.

**Done when**: Package references are updated to supported .NET 10-compatible versions, deprecated or vulnerable package findings are addressed where possible, and restore completes without dependency conflicts.

---

### 03-resolve-compatibility-issues: Resolve source and behavioral compatibility issues

Apply the code and configuration changes required to handle the reported source incompatibilities and behavioral changes after the framework and package upgrades. Focus on the affected API usages identified in the assessment, especially `TimeSpan` factory overloads and ASP.NET Core behavioral changes.

**Done when**: The reported compatibility issues introduced by the .NET 10 upgrade are resolved in code or configuration and the solution compiles cleanly.

---

### 04-validate-solution: Validate the upgraded solution

Run the full solution build and the relevant automated tests to confirm the .NET 10 upgrade is stable across the application, libraries, and test projects.

**Done when**: The solution builds successfully, relevant tests pass, and no remaining upgrade-related validation blockers are found.
