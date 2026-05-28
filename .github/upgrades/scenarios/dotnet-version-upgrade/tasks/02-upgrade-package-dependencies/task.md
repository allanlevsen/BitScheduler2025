# 02-upgrade-package-dependencies: Upgrade package references for .NET 10 compatibility

Update the solution's package references to the recommended .NET 10-compatible versions, including Aspire, ASP.NET Core, Entity Framework Core, Microsoft.Extensions, and OpenTelemetry packages. Address the reported security vulnerability and deprecated package findings as part of this work.

**Done when**: Package references are updated to supported .NET 10-compatible versions, deprecated or vulnerable package findings are addressed where possible, and restore completes without dependency conflicts.

## Research Notes
- Package versions are defined directly in project files; Central Package Management is not enabled.
- `Aspire.AppHost.Sdk`, `Aspire.Hosting.AppHost`, and `Aspire.Hosting.Testing` have supported upgrade targets at `13.3.5`.
- `Microsoft.AspNetCore.OpenApi`, `Microsoft.EntityFrameworkCore`, and `Microsoft.EntityFrameworkCore.SqlServer` have stable .NET 10 targets at `10.0.8`.
- The assessment recommends `10.0.8` for the `Microsoft.Extensions.Configuration*` packages, even though the package lookup tool also exposes newer preview builds. This task will stay on stable .NET 10-aligned package versions rather than moving the solution onto preview dependencies.
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` has a security fix at `1.15.3`, and related OpenTelemetry instrumentation packages have stable `1.15.x` updates.
- `Microsoft.NET.Test.Sdk` can be updated to `18.6.0`; `xunit` can move from `2.9.0` to `2.9.3`.
- `BitScheduleApi` currently gets pruning warnings for `Microsoft.Extensions.Configuration.FileExtensions` and `Microsoft.Extensions.Configuration.Json`; those references appear unnecessary because ASP.NET Core already provides configuration support through the shared framework.

## Planned Package Changes
- Upgrade Aspire packages and SDK to `13.3.5`.
- Upgrade ASP.NET Core, EF Core, Microsoft.Extensions, and OpenTelemetry packages to stable .NET 10-compatible versions.
- Upgrade test packages to current stable compatible versions.
- Remove unnecessary configuration package references from `BitScheduleApi` if restore/build stays clean after the change.

## Validation Plan
1. Update package references in the affected `.csproj` files.
2. Restore the solution and confirm there are no dependency conflicts.
3. Leave code compatibility fixes for the next task if restore succeeds but build warnings or compile issues remain.
