# 05.01-range-entity-and-schema Progress Detail

## Summary
Introduced the new `BitResourceScheduleRange` persistence entity and wired it into EF Core so the solution now has a resource-oriented schedule range table shape alongside the existing `BitDay` model.

## Files Modified
- `BitSchedulerCore/BitResourceScheduleRange.cs`
- `BitSchedulerCore/BitClient.cs`
- `BitSchedulerCore/BitResource.cs`
- `BitSchedulerCore/Data/BitScheduleDbContext.cs`
- `BitSchedulerCore/BitSchedulerCore.csproj`
- `BitSchedulerCore/Migrations/20260528230000_AddBitResourceScheduleRange.cs`
- `BitSchedulerCore/Migrations/20260528230000_AddBitResourceScheduleRange.Designer.cs`
- `BitSchedulerCore/Migrations/BitScheduleDbContextModelSnapshot.cs`

## Validation
- `dotnet build` ✅ succeeded

## Notes
- Added `BitClientId`, `BitResourceId`, `StartDate`, `EndDate`, and binary `Payload` storage to the new entity.
- Added navigation properties from both `BitClient` and `BitResource`.
- Added unique and overlap-search-friendly indexes for the new schedule range table.
- `dotnet ef migrations add` could not run successfully in this environment because of an EF tooling/runtime mismatch, so the migration files were authored manually to keep the schema in sync with the code changes.
