# 01-update-target-frameworks: Update target frameworks across all projects

Move every project in the solution from `net9.0` to `net10.0` so the solution is consistently aligned on the new framework baseline. This includes application, library, and test projects.

**Done when**: Every project file in the solution targets .NET 10 and solution restore still succeeds.

## Research Notes
- All 8 projects define `TargetFramework` directly in their `.csproj` files.
- No `Directory.Build.props` or `Directory.Build.targets` file was found overriding target frameworks.
- All projects are currently single-targeted and should remain single-targeted.
- This task only updates target framework declarations; package version alignment and code fixes are handled by later tasks.

## Affected Projects
- `AspireBitSchedule.ApiService/AspireBitSchedule.ApiService.csproj`
- `AspireBitSchedule.AppHost/AspireBitSchedule.AppHost.csproj`
- `AspireBitSchedule.ServiceDefaults/AspireBitSchedule.ServiceDefaults.csproj`
- `AspireBitSchedule.Tests/AspireBitSchedule.Tests.csproj`
- `AspireBitSchedule.Web/AspireBitSchedule.Web.csproj`
- `BitScheduleApi/BitScheduleApi.csproj`
- `BitSchedulerCore/BitSchedulerCore.csproj`
- `BitTimeScheduler/BitTimeScheduler.csproj`

## Execution Plan
1. Replace `net9.0` with `net10.0` in each project file.
2. Run solution restore to verify the retargeted projects still restore.
3. Record results in `progress-detail.md` before completing the task.
