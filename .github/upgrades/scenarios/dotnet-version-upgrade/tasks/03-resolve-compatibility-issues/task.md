# 03-resolve-compatibility-issues: Resolve source and behavioral compatibility issues

Apply the code and configuration changes required to handle the reported source incompatibilities and behavioral changes after the framework and package upgrades. Focus on the affected API usages identified in the assessment, especially `TimeSpan` factory overloads and ASP.NET Core behavioral changes.

**Done when**: The reported compatibility issues introduced by the .NET 10 upgrade are resolved in code or configuration and the solution compiles cleanly.

## Research Notes
- A full solution build succeeds after the framework and package updates, so the assessment findings are compatibility risks rather than current compiler breaks.
- The source-compatibility findings are concentrated around `TimeSpan.FromHours(int)` and `TimeSpan.FromMinutes(long)` usages in `BitScheduleApi`, `BitSchedulerCore`, and `BitTimeScheduler` test code.
- These calls can be made unambiguous and framework-stable by using `double` arguments instead of integer arithmetic overloads.
- The ASP.NET Core behavioral findings are tied to `UseExceptionHandler()` in `AspireBitSchedule.ApiService` and the `createScopeForErrors` overload in `AspireBitSchedule.Web`.
- The safest low-impact change is to use more explicit exception-handler configuration while preserving current application behavior.

## Planned Changes
- Replace flagged `TimeSpan` factory calls with `double`-based arguments.
- Change `AspireBitSchedule.ApiService` to use an explicit `/error` exception-handler endpoint that returns problem details.
- Simplify `AspireBitSchedule.Web` to the `UseExceptionHandler("/Error")` overload.

## Validation Plan
1. Apply the targeted compatibility fixes.
2. Build the full solution to confirm the upgrade still compiles.
3. Leave broader runtime validation to the final validation task.
